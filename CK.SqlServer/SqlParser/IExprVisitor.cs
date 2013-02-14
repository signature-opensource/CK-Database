using System;
using CK.Core;

namespace CK.SqlServer
{
    public interface IExprVisitor<T>
    {
        T VisitExpr( SqlExpr e );

        T Visit( SqlExprIdentifier e );
        T Visit( SqlExprMultiIdentifier e );
        T Visit( SqlAssignExpr e );
        T Visit( SqlExprNull e );
        T Visit( SqlExprTypeDecimal e );
        T Visit( SqlExprTypeDateAndTime e );
        T Visit( SqlExprTypeSimple e );
        T Visit( SqlExprTypeWithSize e );
        T Visit( SqlExprSyntaxError e );
        T Visit( SqlExprTypeUserDefined e );
        T Visit( SqlExprType e );
        T Visit( SqlExprTypedIdentifier e );
        T Visit( SqlExprParameter e );
        T Visit( SqlExprParameterList e );   
    }
}
