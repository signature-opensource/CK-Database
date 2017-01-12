using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res.Poco" ), Versions( "1.0.0" )]
    public abstract class PocoPackage : SqlPackage
    {
        [SqlProcedure( "sPocoThingWrite" )]
        public abstract string Write( SqlStandardCallContext ctx, [ParameterSource]IThing thing );
    }
}
