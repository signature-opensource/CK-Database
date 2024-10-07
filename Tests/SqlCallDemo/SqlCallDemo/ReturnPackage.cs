using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;

namespace SqlCallDemo;


[SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
public abstract partial class ReturnPackage : SqlPackage
{
    [SqlProcedure( "sStringReturn" )]
    public abstract string StringReturn( SqlStandardCallContext ctx, int v );

    [SqlProcedure( "sStringReturn" )]
    public abstract Task<string> StringReturnAsync( SqlStandardCallContext ctx, int v );

    [SqlProcedure( "sIntReturn" )]
    public abstract int IntReturn( SqlStandardCallContext ctx, int? v );

    [SqlProcedure( "sIntReturn" )]
    public abstract Task<int> IntReturnAsync( SqlStandardCallContext ctx, int? v );

    [SqlProcedure( "sIntReturnWithActor" )]
    public abstract int IntReturnWithActor( [ParameterSource] IActorCallContext ctx, string def = "5" );

    [SqlProcedure( "sIntReturnWithActor" )]
    public abstract Task<int> IntReturnWithActorAsync( [ParameterSource] IActorCallContext ctx, string def = "5" );

}
