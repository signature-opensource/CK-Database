using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using CK.Setup;
using System.Text;
using System.Diagnostics;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Implements <see cref="IVersionedItemWriter"/> on a Sql Server database.
    /// </summary>
    public class SqlVersionedItemWriter : IVersionedItemWriter
    {
        readonly ISqlManagerBase _manager;
        bool _initialized;

        /// <summary>
        /// Writes Initializes a new <see cref="SqlVersionedItemWriter"/>.
        /// </summary>
        /// <param name="m">The sql manager to use.</param>
        public SqlVersionedItemWriter( ISqlManagerBase m )
        {
            Throw.CheckNotNullArgument( m );
            _manager = m;
        }

        /// <inheritdoc />
        public void SetVersions( IActivityMonitor monitor,
                                 IVersionedItemReader reader,
                                 IEnumerable<VersionedNameTracked> trackedItems,
                                 bool deleteUnaccessedItems,
                                 IReadOnlyCollection<VFeature> originalFeatures,
                                 IReadOnlyCollection<VFeature> finalFeatures,
                                 in SHA1Value runSignature )
        {
            var sqlReader = reader as SqlVersionedItemReader;
            bool rewriteToSameDatabase = sqlReader != null && sqlReader.Manager == _manager;
            if( !rewriteToSameDatabase && !_initialized && _manager is ISqlManager actualManager )
            {
                SqlVersionedItemReader.AutoInitialize( actualManager );
                _initialized = true;
            }
            StringBuilder delete = null;
            StringBuilder deleteTrace = null;
            StringBuilder update = null;

            void Delete( string fullName, bool hasBeenAccessed, string type, string version )
            {
                if( delete == null ) delete = new StringBuilder( "delete from CKCore.tItemVersionStore where FullName in (" );
                else delete.Append( ',' );
                delete.Append( "N'" ).Append( SqlHelper.SqlEncodeStringContent( fullName ) ).Append( '\'' );

                if( deleteTrace == null )
                {
                    deleteTrace = new StringBuilder();
                    deleteTrace.AppendLine( "Items deletion from CKCore.tItemVersionStore:" ).AppendLine();
                }
                deleteTrace.Append( "Item ('" ).Append( fullName ).Append( "','" ).Append( type ).Append( "','" ).Append( version ).Append( "') " );
                if( !hasBeenAccessed ) deleteTrace.AppendLine( "--> Unaccessed during DBSetup." );
                else deleteTrace.AppendLine( "--> Explicitly deleted." );
            }

            void Update( string fullName, string typeName, string version )
            {
                if( update == null )
                {
                    update = new StringBuilder( SqlVersionedItemReader.CreateTemporaryTableScript );
                    update.AppendLine().Append( "insert into @T( F, T, V ) values " );
                }
                else update.Append( ',' );
                update.Append( "(N'" ).Append( SqlHelper.SqlEncodeStringContent( fullName ) )
                    .Append( "','" ).Append( SqlHelper.SqlEncodeStringContent( typeName ) )
                    .Append( "','" ).Append( version ).Append( "')" );
            }

            if( runSignature != reader.GetSignature( monitor ) )
            {
                Update( "RunSignature", "RunSignature", runSignature.ToString() );
            }

            foreach( VersionedNameTracked t in trackedItems )
            {
                bool mustDelete = t.Deleted || (deleteUnaccessedItems && !t.Accessed);
                if( mustDelete )
                {
                    Delete( t.FullName, t.Accessed, t.Original.Type, t.Original.Version.ToString() );
                }
                else if( t.Original == null )
                {
                    monitor.Trace( $"Item '{t.FullName}' is a new one." );
                    Update( t.FullName, t.NewType, t.NewVersion.ToString() );
                }
                else if( t.NewVersion != null )
                {
                    bool mustUpdate = false;
                    if( t.NewVersion > t.Original.Version )
                    {
                        monitor.Trace( $"Item '{t.FullName}': version is upgraded." );
                        mustUpdate = true;
                    }
                    else if( t.NewVersion < t.Original.Version )
                    {
                        monitor.Warn( $"Item '{t.FullName}': version downgraded from {t.Original.Version} to {t.NewVersion}." );
                        mustUpdate = true;
                    }
                    if( t.NewType != t.Original.Type )
                    {
                        monitor.Warn( $"Item '{t.FullName}': Type change from '{t.Original.Type}' to '{t.NewType}'." );
                        mustUpdate = true;
                    }
                    if( mustUpdate ) Update( t.FullName, t.NewType, t.NewVersion.ToString() );
                }
                else
                {
                    if( !rewriteToSameDatabase )
                    {
                        monitor.Trace( $"Item '{t.FullName}' has not been accessed. Updating target database." );
                        Update( t.Original.FullName, t.Original.Type, t.Original.Version.ToString() );
                    }
                }
            }

            var featureDiff = originalFeatures.Select( o => (o.Name, O: o, F: new VFeature()) )
                                .Concat( finalFeatures.Select( f => (f.Name, O: new VFeature(), F: f) ) )
                                .GroupBy( t => t.Name )
                                .Select( x => (Name: x.Key, x.First().O, x.Last().F) );
            foreach( var f in featureDiff )
            {
                if( !f.O.IsValid )
                {
                    monitor.Info( $"Created VFeature: '{f.F}'." );
                    Update( f.Name, "VFeature", f.F.Version.ToNormalizedString() );
                }
                else if( !f.F.IsValid )
                {
                    monitor.Info( $"Removed VFeature: '{f.O}'." );
                    Delete( f.Name, false, "VFeature", f.O.Version.ToNormalizedString() );
                }
                else if( f.O.Version != f.F.Version )
                {
                    monitor.Info( $"Updated VFeature {f.O} to version {f.F.Version}." );
                    Update( f.Name, "VFeature", f.F.Version.ToNormalizedString() );
                }
                else monitor.Debug( $"VFeature {f.O} unchanged." );
            }

            // Throws exception on error, but uses ExecuteOneScript with monitor because it
            // ensures that failing script is logged on error.
            if( deleteTrace != null )
            {
                monitor.UnfilteredLog( LogLevel.Info, null, deleteTrace.ToString(), null );

                Debug.Assert( delete != null );
                delete.Append( ");" );
                if( !_manager.ExecuteOneScript( delete.ToString(), monitor ) )
                {
                    throw new Exception( $"Unable to apply required deletions. Detailed error (including failing script) has been logged." );
                }
            }
            else monitor.Trace( "No version deleted." );
            if( update != null )
            {
                update.AppendLine( ";" ).Append( SqlVersionedItemReader.MergeTemporaryTableScript );
                if( !_manager.ExecuteOneScript( update.ToString(), monitor ) )
                {
                    throw new Exception( $"Unable to apply required updates. Detailed error (including failing script) has been logged." );
                }
            }
        }
    }
}
