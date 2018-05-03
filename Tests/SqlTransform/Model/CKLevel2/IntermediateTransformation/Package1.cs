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
    [SqlObjectItem("vBase")]
    public abstract class Package1 : SqlPackage
    {
        public List<int> ReadViewBase( ISqlCallContext ctx )
        {
            using( var cmd = new SqlCommand("select KeyValue from ITrans.vBase"))
            {
                return ctx[Database].ExecuteReader( cmd, r => r.GetInt32( 0 ) );
            }
        }
    }
}
