using CK.SqlServer;
using CK.Core;

namespace SqlCallDemo;


[SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
public abstract partial class MiscPackage : SqlPackage
{
    [SqlProcedure( "sSleepProc" )]
    public abstract void CanWaitForTheDefaultCommandTimeout( SqlStandardCallContext ctx, int sleepTime );

    [SqlProcedure( "sSleepProc", TimeoutSeconds = 1 )]
    public abstract void CanWaitOnlyForOneSecond( SqlStandardCallContext ctx, int sleepTime );

    [SqlProcedure( "sVerbatimParameterProc" )]
    public abstract int VerbatimParameterAtWork( SqlStandardCallContext ctx, int @this, int @operator );

}
