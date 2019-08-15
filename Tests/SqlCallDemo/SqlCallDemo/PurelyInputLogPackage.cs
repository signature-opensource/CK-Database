using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class PurelyInputLogPackage : SqlPackage
    {
        [SqlProcedure( "sPurelyInputSimpleLog" )]
        public abstract Task SimpleLog( SqlStandardCallContext ctx, string logText );

        [SqlProcedure( "sPurelyInputLog" )]
        public abstract Task Log( SqlStandardCallContext ctx, bool? oneMore, string logText );

        [SqlProcedure( "sPurelyInputLog" )]
        public abstract Task LogWithDefaultBitValue( SqlStandardCallContext ctx, string logText );

        [SqlProcedure( "sPurelyInputLog" )]
        public abstract Task LogWait( SqlStandardCallContext ctx, string logText, int waitTimeMS, CancellationToken cancellationToken );

    }
}
