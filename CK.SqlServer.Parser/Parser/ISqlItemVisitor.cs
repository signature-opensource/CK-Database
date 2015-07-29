#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\ISqlItemVisitor.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public interface ISqlItemVisitor<T>
    {
        T VisitItem( SqlItem e );

        T Visit( SqlExprUnmodeledItems e );
        T Visit( SqlExprRawItemList e );
        T Visit( SqlExprKoCall e );
        T Visit( SqlNoExprOverClause e );
        T Visit( SqlExprCollate e );
        T Visit( SqlExprStIf e );

        T Visit( SqlExprCursor e );
        T Visit( SqlNoExprIdentifierList e );
        T Visit( SqlExprCursorSql92 e );
        T Visit( SqlExprStDeclareCursor e );

        T Visit( SqlExprStBeginTran e );
        T Visit( SqlExprStatementList e );
        T Visit( SqlExprStBlock e );
        T Visit( SqlExprStTryCatch e );       
        T Visit( SqlExprStUnmodeled e );
        T Visit( SqlExprStStoredProc e );
        T Visit( SqlExprStFunctionScalar e );
        T Visit( SqlExprStReturn e );
        T Visit( SqlExprStSetVar e );
        T Visit( SqlExprStSetOpt e );
        T Visit( SqlExprStGoto e );
        T Visit( SqlExprStMonoStatement e );
        T Visit( SqlExprStLabelDef e );
        T Visit( SqlExprStEmpty e );
        T Visit( SqlExprStView e );
        T Visit( SqlExprColumnList e );
        T Visit( SqlNoExprExecuteAs e );
        T Visit( SqlExprStDeclare e );
        T Visit( SqlExprDeclareList e );       
        T Visit( SqlExprDeclare e );
        
        T Visit( SqlExprCast e );
        T Visit( SqlExprIdentifier e );
        T Visit( SqlExprMultiIdentifier e );
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

        T Visit( SelectQuery e );
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
        T Visit( SelectOrderByColumnList e );
        T Visit( SelectOrderByColumn e );
        T Visit( SelectOrderByOffset e );
        
        T Visit( SelectFor e );
        T Visit( SelectOption e );

        T Visit( SqlExprStFunctionInlineTable e );
        

    }
}
