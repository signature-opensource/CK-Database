using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CK.Core;
using CK.Setup;
using System.Diagnostics;
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

        public void SetVersions( IActivityMonitor monitor, IVersionedItemReader reader, IEnumerable<VersionedNameTracked> trackedItems, bool deleteUnaccessedItems )
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
            foreach( VersionedNameTracked t in trackedItems )
            {
                VersionedTypedName toSet = null;
                bool mustDelete = t.Deleted || (deleteUnaccessedItems && !t.Accessed);
                if( mustDelete )
                {
                    if( delete == null ) delete = new StringBuilder( "delete from CKCore.tItemVersionStore where FullName in (" );
                    else delete.Append( ',' );
                    delete.Append( "N'" ).Append( SqlHelper.SqlEncodeStringContent( t.FullName ) ).Append( '\'' );
                    if( !t.Accessed )
                    {
                        monitor.Info( $"Item '{t.FullName}' has not been accessed: deleting its version information." );
                    }
                    else monitor.Info( $"Deleting '{t.FullName}' version information." );
                }
                else if( t.Original == null )
                {
                    monitor.Trace( $"Item '{t.FullName}' is a new one." );
                    toSet = new VersionedTypedName( t.FullName, t.NewType, t.NewVersion );
                }
                else if( t.NewVersion != null )
                {
                    if( t.NewVersion > t.Original.Version )
                    {
                        monitor.Trace( $"Item '{t.FullName}': version is upgraded." );
                        toSet = new VersionedTypedName( t.FullName, t.NewType, t.NewVersion );
                    }
                    else if( t.NewVersion < t.Original.Version )
                    {
                        monitor.Error( $"Item '{t.FullName}': version downgraded from {t.Original.Version} to {t.NewVersion}. This is ignored." );
                    }
                    else if( t.NewType != t.Original.Type )
                    {
                        monitor.Error( $"Item '{t.FullName}': Type change from '{t.Original.Type}' to '{t.NewType}'. This is ignored." );
                    }
                }
                else
                {
                    if( !rewriteToSameDatabase )
                    {
                        monitor.Trace( $"Item '{t.FullName}' has not been accessed. Updating target database." );
                        toSet = t.Original;
                    }
                }
                if( toSet != null )
                {
                    if( update == null )
                    {
                        update = new StringBuilder( SqlVersionedItemReader.CreateTemporaryTableScript );
                        update.Append( "insert into @T( F, T, V ) values " );
                    }
                    else update.Append( ',' );
                    update.Append("(N'").Append( SqlHelper.SqlEncodeStringContent( toSet.FullName ) )
                        .Append( "','" ).Append( SqlHelper.SqlEncodeStringContent( toSet.Type ) )
                        .Append( "','" ).Append( toSet.Version.ToString() ).Append( "')" );
                }
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
