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

    [SqlPackage( Schema = "CK", Database = typeof( SqlDefaultDatabase ), ResourceType = typeof( PurelyInputLogPackage ), ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class PurelyInputLogPackage : SqlPackage
    {
        [SqlProcedure( "sPurelyInputSimpleLog", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Task SimpleLog( SqlStandardCallContext ctx, string logText );

        [SqlProcedure( "sPurelyInputLog", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Task Log( SqlStandardCallContext ctx, bool? oneMore, string logText );

        [SqlProcedure( "sPurelyInputLog", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Task LogWithDefaultBitValue( SqlStandardCallContext ctx, string logText );

        [SqlProcedure( "sPurelyInputLog", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Task LogWait( SqlStandardCallContext ctx, string logText, int waitTimeMS, CancellationToken cancellationToken );

    }
}
