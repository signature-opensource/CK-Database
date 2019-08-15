using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using CK.Setup;
using System.Text;

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
            if( m == null ) throw new ArgumentNullException( nameof( m ) );
            _manager = m;
        }

        /// <summary>
        /// Updates the version informations or throws an exception if anything prevents the versions
        /// to be correctly updated.
        /// When <paramref name="deleteUnaccessedItems"/> is true, any non accessed names can be removed (if they exist)
        /// otherwise only names that has been deleted or have a new version should be updated.
        /// </summary>
        /// <param name="monitor">Monitor to use for warnings or informations. Exceptions should be thrown an any serious error.</param>
        /// <param name="reader">
        /// The reader that has been used to read the original versions: this can be used to enable 
        /// checks and/or optimizations.
        /// </param>
        /// <param name="trackedItems">The set of <see cref="VersionedNameTracked"/> objects.</param>
        /// <param name="deleteUnaccessedItems">True to delete unaccessed items.</param>
        /// <param name="originalFeatures">The original features.</param>
        /// <param name="finalFeatures">The final (current) features.</param>
        public void SetVersions(
            IActivityMonitor monitor,
            IVersionedItemReader reader,
            IEnumerable<VersionedNameTracked> trackedItems,
            bool deleteUnaccessedItems,
            IReadOnlyCollection<VFeature> originalFeatures,
            IReadOnlyCollection<VFeature> finalFeatures )
        {
            var sqlReader = reader as SqlVersionedItemReader;
            bool rewriteToSameDatabase = sqlReader != null && sqlReader.Manager == _manager;
            if( !rewriteToSameDatabase && !_initialized && _manager is ISqlManager actualManager )
            {
                SqlVersionedItemReader.AutoInitialize( actualManager );
                _initialized = true;
            }
            StringBuilder delete = null;
            StringBuilder update = null;

            void Delete( string fullName, bool hasBeenAccessed )
            {
                if( delete == null ) delete = new StringBuilder( "delete from CKCore.tItemVersionStore where FullName in (" );
                else delete.Append( ',' );
                delete.Append( "N'" ).Append( SqlHelper.SqlEncodeStringContent( fullName ) ).Append( '\'' );
                if( !hasBeenAccessed )
                {
                    monitor.Info( $"Item '{fullName}' has not been accessed: deleting its version information." );
                }
                else monitor.Info( $"Deleting '{fullName}' version information." );
            }

            void Update( string fullName, string typeName, string version )
            {
                if( update == null )
                {
                    update = new StringBuilder( SqlVersionedItemReader.CreateTemporaryTableScript );
                    update.Append( "insert into @T( F, T, V ) values " );
                }
                else update.Append( ',' );
                update.Append( "(N'" ).Append( SqlHelper.SqlEncodeStringContent( fullName ) )
                    .Append( "','" ).Append( SqlHelper.SqlEncodeStringContent( typeName ) )
                    .Append( "','" ).Append( version ).Append( "')" );
            }

            foreach( VersionedNameTracked t in trackedItems )
            {
                bool mustDelete = t.Deleted || (deleteUnaccessedItems && !t.Accessed);
                if( mustDelete )
                {
                    Delete( t.FullName, t.Accessed );
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
                    monitor.Info( $"Created feature: '{f.F}'." );
                    Update( f.Name, "VFeature", f.F.Version.ToNuGetPackageString() );
                }
                else if( !f.F.IsValid )
                {
                    monitor.Info( $"Removed feature: '{f.O}'." );
                    Delete( f.Name, false );
                }
                else if( f.O.Version != f.F.Version )
                {
                    monitor.Info( $"Updated feature {f.O} to version {f.F.Version}." );
                    Update( f.Name, "VFeature", f.F.Version.ToNuGetPackageString() );
                }
                else monitor.Debug( $"Feature {f.O} unchanged." );
            }

            if( delete != null )
            {
                delete.Append( ");" );
                // Throws exception on error.
                _manager.ExecuteOneScript( delete.ToString() );
            }
            if( update != null )
            {
                update.Append( ";" ).Append( SqlVersionedItemReader.MergeTemporaryTableScript );
                // Throws exception on error.
                _manager.ExecuteOneScript( update.ToString() );
            }
        }

    }
}
