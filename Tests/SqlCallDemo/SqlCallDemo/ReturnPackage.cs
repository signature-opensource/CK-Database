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

    [SqlPackage( Schema = "CK", Database = typeof( SqlDefaultDatabase ), ResourceType = typeof( ReturnPackage ), ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class ReturnPackage : SqlPackage
    {
        [SqlProcedure( "sStringReturn", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Task<string> StringReturn( SqlStandardCallContext ctx, int v );

        [SqlProcedure( "sIntReturn", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Task<int> IntReturn( SqlStandardCallContext ctx, int? v );

    }
}
