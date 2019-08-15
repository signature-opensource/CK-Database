using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CKLevel2.IntermediateTransformation
{
    [SqlPackage( ResourcePath = "Res", Schema = "ITrans" )]
    [Versions("0.0.0")]
    [SqlObjectItem( "transform:vBase" )]
    public abstract class Package3 : SqlPackage
    {
        void StObjConstruct( Package2 p1 )
        {
        }

        public List<Tuple<int, string, string>> ReadViewBase(ISqlCallContext ctx)
        {
            using (var cmd = new SqlCommand("select KeyValue, name, Type from ITrans.vBase"))
            {
                return ctx[Database].ExecuteReader( cmd, r => Tuple.Create( r.GetInt32( 0 ), r.GetString( 1 ), r.GetString( 2 ) ) );
            }
        }

    }
}
