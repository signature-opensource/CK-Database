using System;
using CK.Core;

namespace CK.SqlServer
{
    public interface IExprVisitor<T>
    {
        T VisitExpr( SqlExpr e );

        T Visit( SqlExprUnmodeledTokens e );

        T Visit( SqlExprStatementList e );
        T Visit( SqlExprStBlock e );       
        T Visit( SqlExprStUnmodeled e );
        T Visit( SqlExprStStoredProc e );

        T Visit( SqlExprIdentifier e );
        T Visit( SqlExprMultiIdentifier e );
        T Visit( SqlExprTerminal e );
        T Visit( SqlExprLiteral e );
        T Visit( SqlExprNull e );
        T Visit( SqlExprUnaryOperator e );
       
        T Visit( SqlExprTypeDecl e );
        T Visit( SqlExprTypeDeclDecimal e );
        T Visit( SqlExprTypeDeclDateAndTime e );
        T Visit( SqlExprTypeDeclSimple e );
        T Visit( SqlExprTypeDeclWithSize e );
        T Visit( SqlExprSyntaxError e );
        T Visit( SqlExprTypeDeclUserDefined e );
        
        T Visit( SqlExprTypedIdentifier e );
        T Visit( SqlExprParameter e );
        T Visit( SqlExprParameterDefaultValue e );
        T Visit( SqlExprParameterList e );
        
        T Visit( SqlAssignExpr e );
    }
}
