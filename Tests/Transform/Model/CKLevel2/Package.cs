using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKLevel2
{
    [SqlPackage( ResourcePath = "Res", Schema = "CK" )]
    [Versions("0.0.0")]
    [SqlObjectItem( "transform:sSimpleTransformTest" )]
    public abstract class Package : SqlPackage
    {
        [SqlProcedureNonQuery( "replace:sSimpleReplaceTest" )]
        public abstract string SimpleReplaceTest( ISqlCallContext ctx, string textParam, int added );
    }
}
