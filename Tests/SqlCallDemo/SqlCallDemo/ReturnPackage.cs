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
        [SqlProcedureNonQuery( "sStringReturn" )]
        public abstract string StringReturn( SqlStandardCallContext ctx, int v );

        [SqlProcedureNonQuery( "sStringReturn" )]
        public abstract Task<string> StringReturnAsync( SqlStandardCallContext ctx, int v );

        [SqlProcedureNonQuery( "sIntReturn" )]
        public abstract int IntReturn( SqlStandardCallContext ctx, int? v );

        [SqlProcedureNonQuery( "sIntReturn" )]
        public abstract Task<int> IntReturnAsync( SqlStandardCallContext ctx, int? v );

        [SqlProcedureNonQuery( "sIntReturnWithActor" )]
        public abstract int IntReturnWithActor( IActorCallContextIsExecutor ctx, string def = "5" );

        [SqlProcedureNonQuery( "sIntReturnWithActor" )]
        public abstract Task<int> IntReturnWithActorAsync( IActorCallContextIsExecutor ctx, string def = "5" );

        [SqlProcedureNonQuery( "sIntReturnWithActor" )]
        public abstract int IntReturnWithActor( IActorCallContext ctx, string def = "5" );

        [SqlProcedureNonQuery( "sIntReturnWithActor" )]
        public abstract Task<int> IntReturnWithActorAsync( IActorCallContext ctx, string def = "5" );

    }
}
