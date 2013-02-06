using System;
using CK.Core;

namespace CK.SqlServer
{
    public interface IExprVisitor<T>
    {
        T VisitExpr( SqlExpr e );

        T Visit( SqlAssignExpr e );
        T Visit( SqlIdentifierExpr e );
        T Visit( SqlLiteralFloatExpr e );
        T Visit( SqlLiteralIntegerExpr e );
        T Visit( SqlLiteralMoneyExpr e );
        T Visit( SqlLiteralNumericExpr e );
        T Visit( SqlLiteralStringExpr e );
        T Visit( SqlNullExpr e );
        T Visit( SqlParameterExpr e );
        T Visit( SqlParameterListExpr e );
        T Visit( SqlSyntaxErrorExpr e );
        T Visit( SqlTypedIdentifierExpr e );
        T Visit( SqlTypeExpr e );
        T Visit( SqlCommentExpr e );   
    }
}
