using System;
using CK.Core;

namespace CK.SqlServer
{
    public interface IExprVisitor<T>
    {
        T VisitExpr( SqlItem e );

        T Visit( SqlExprUnmodeledTokens e );
        T Visit( SqlExprRawItemList e );
        T Visit( SqlExprKoCall e );
        T Visit( SqlExprStIf e );
        
        T Visit( SqlExprStatementList e );
        T Visit( SqlExprStBlock e );       
        T Visit( SqlExprStUnmodeled e );
        T Visit( SqlExprStStoredProc e );
        T Visit( SqlExprStMonoStatement e );
        T Visit( SqlExprStLabelDef e );
        T Visit( SqlExprStEmpty e );
        T Visit( SqlExprStView e );
        T Visit( SqlExprColumnList e );

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
        T Visit( SqlExprTypeDeclUserDefined e );
        
        T Visit( SqlExprTypedIdentifier e );
        T Visit( SqlExprParameter e );
        T Visit( SqlExprParameterDefaultValue e );
        T Visit( SqlExprParameterList e );
        
        T Visit( SqlExprAssign e );
        T Visit( SqlExprBinaryOperator e );
        T Visit( SqlExprIsNull e );
        T Visit( SqlExprLike e );
        T Visit( SqlExprBetween e );
        T Visit( SqlExprIn e );
        T Visit( SqlExprCase e );
        T Visit( SqlExprCaseWhenSelector e );

        T Visit( SqlExprCommaList e );

        T Visit( SelectSpecification e );
        T Visit( SelectHeader e );
        T Visit( SelectColumnList e );
        T Visit( SelectColumn e );
        T Visit( SelectInto e );
        T Visit( SelectFrom e );
        T Visit( SelectWhere e );
        T Visit( SelectGroupBy e );
        T Visit( SelectCombineOperator e );
        T Visit( SelectOrderBy e );
        T Visit( SelectFor e );
        T Visit( SelectOption e );
        

        T Visit( SelectQuery e );
    }
}
