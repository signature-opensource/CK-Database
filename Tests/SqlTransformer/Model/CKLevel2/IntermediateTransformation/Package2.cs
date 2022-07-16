using CK.Core;
using CK.SqlServer;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace CKLevel2.IntermediateTransformation
{
    [SqlPackage( ResourcePath = "Res", Schema = "ITrans" )]
    [Versions("0.0.0")]
    [SqlObjectItem( "transform:vBase" )]
    [SqlObjectItem( "vDependent" )]
    public abstract class Package2 : SqlPackage
    {
        void StObjConstruct( Package1 p1 )
        {
        }

        public List<KeyValuePair<int,string>> ReadViewBase( ISqlCallContext ctx )
        {
            using( var cmd = new SqlCommand( "select KeyValue, name from ITrans.vBase" ) )
            {
                return ctx[Database].ExecuteReader( cmd, r =>
                    new KeyValuePair<int, string>( r.GetInt32( 0 ), r.GetString( 1 ) ) );
            }
        }

    }
}
