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

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class ReturnPackage : SqlPackage
    {
        [SqlProcedure( "sStringReturn", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Task<string> StringReturnAsync( SqlStandardCallContext ctx, int v );

        [SqlProcedure( "sIntReturn", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Task<int> IntReturnAsync( SqlStandardCallContext ctx, int? v );

        [SqlProcedure( "sIntReturnWithActor", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Task<int> IntReturnWithActorAsync( IActorCallContext ctx );

    }
}
