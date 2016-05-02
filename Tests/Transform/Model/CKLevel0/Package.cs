using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKLevel0
{
    [SqlPackage( ResourcePath ="Res", Schema = "CK")]
    [Versions("0.0.0")]
    public abstract class Package : SqlPackage
    {
        [SqlProcedureNonQuery("sTest")]
        public abstract string Test( ISqlCallContext ctx, string textParam );
    }
}
