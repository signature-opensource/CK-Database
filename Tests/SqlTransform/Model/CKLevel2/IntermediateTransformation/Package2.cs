using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
