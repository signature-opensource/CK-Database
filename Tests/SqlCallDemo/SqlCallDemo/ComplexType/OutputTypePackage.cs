using CK.Core;
using CK.SqlServer;

namespace SqlCallDemo.ComplexType
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "21.2.19" )]
    public abstract partial class OutputTypePackage : SqlPackage
    {
        [SqlProcedure( "sOutputTypeCastWithDefault" )]
        public abstract OutputTypeCastWithDefault GetWithSqlDefault( ISqlCallContext ctx );

        [SqlProcedure( "sOutputTypeCastWithDefault" )]
        public abstract OutputTypeCastWithDefault GetNoDefault( ISqlCallContext ctx, int paramInt, short paramSmallInt, sbyte paramTinyInt );

        [SqlProcedure( "sOutputTypeCastWithDefault" )]
        public abstract OutputTypeCastWithDefault GetWithCSharpDefault( ISqlCallContext ctx, int paramInt, short paramSmallInt = 37, sbyte paramTinyInt = 12 );

    }
}
