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
    public class SqlVersionedItemWriter : IVersionedItemWriter
    {
        readonly ISqlManagerBase _manager;

        public SqlVersionedItemWriter( ISqlManagerBase m )
        {
            if( m == null ) throw new ArgumentNullException( nameof( m ) );
            _manager = m;
        }

        public void SetVersions( IActivityMonitor monitor, IVersionedItemReader reader, IEnumerable<VersionedNameTracked> trackedItems, bool deleteUnaccessedItems )
        {
            var sqlReader = reader as SqlVersionedItemReader;
            bool rewriteToSameDatabase = sqlReader != null && sqlReader.Manager == _manager;
            StringBuilder delete = null;
            StringBuilder update = null;
            foreach( var t in trackedItems )
            {
                if( t.Deleted || (!t.Accessed && deleteUnaccessedItems) )
                {
                    if( delete == null ) delete = new StringBuilder( "delete from CKCore.tItemVersionStore where FullName in (" );
                    else delete.Append( ',' );
                    delete.Append( "N'" ).Append( SqlHelper.SqlEncodeStringContent( t.FullName ) ).Append( '\'' ); 
                }
                else
                {
                    if( t.NewVersion != null
                        && (!rewriteToSameDatabase || t.NewVersion != t.Original?.Version || t.NewType != t.Original?.Type) )
                    {
                        if( update == null )
                        {
                            update = new StringBuilder( SqlVersionedItemReader.CreateTemporaryTableScript );
                            update.Append( "insert into @T( F, T, V ) values " );
                        }
                        else update.Append( ',' );
                        update.Append("(N'").Append( SqlHelper.SqlEncodeStringContent( t.FullName ) )
                            .Append( "','" ).Append( SqlHelper.SqlEncodeStringContent( t.NewType ) )
                            .Append( "','" ).Append( t.NewVersion.ToString() ).Append( "')" );
                    }
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
