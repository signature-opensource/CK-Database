using CK.Core;
using CK.SqlServer;

namespace SqlCallDemo.ComplexType
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "21.2.19" )]
    public abstract partial class IOTypePackage : SqlPackage
    {
        [SqlProcedure( "sOutputTypeCastWithDefault" )]
        public abstract OutputTypeCastWithDefault GetWithSqlDefault( ISqlCallContext ctx );

        [SqlProcedure( "sOutputTypeCastWithDefault" )]
        public abstract OutputTypeCastWithDefault GetNoDefault( ISqlCallContext ctx, int paramInt, short paramSmallInt, byte paramTinyInt );

        [SqlProcedure( "sOutputTypeCastWithDefault" )]
        public abstract OutputTypeCastWithDefault GetWithCSharpDefault( ISqlCallContext ctx, int paramInt, short paramSmallInt = 37, byte paramTinyInt = 12 );

        [SqlProcedure( "sOutputTypeCastWithDefault" )]
        public abstract string GetWithInputType( ISqlCallContext ctx, [ParameterSource]InputTypeCastWithDefault p );

    }
}
