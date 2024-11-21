using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;

namespace SqlCallDemo.ComplexType;


public abstract partial class ComplexTypePackage : SqlPackage
{
    [SqlProcedure( "sComplexTypeStupidEmpty" )]
    public abstract Task<ComplexTypeStupidEmpty> GetComplexTypeStupidEmptyAsync( ISqlCallContext ctx );

    [SqlProcedure( "sComplexTypeSimple" )]
    public abstract Task<ComplexTypeSimple> GetComplexTypeSimpleAsync( ISqlCallContext ctx, int id = 0 );

    [SqlProcedure( "sComplexTypeSimple" )]
    public abstract Task<ComplexTypeSimpleWithCtor> GetComplexTypeSimpleWithCtorAsync( ISqlCallContext ctx, int id = 0 );

    [SqlProcedure( "sComplexTypeSimple" )]
    public abstract Task<ComplexTypeSimpleWithExtraProperty> GetComplexTypeSimpleWithExtraPropertyAsync( ISqlCallContext ctx, int id = 0 );

    [SqlProcedure( "sComplexTypeSimple" )]
    public abstract Task<ComplexTypeSimpleWithMissingProperty> GetComplexTypeSimpleWithMissingPropertyAsync( ISqlCallContext ctx, int id = 0 );

}
