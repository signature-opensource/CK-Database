using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public class SqlItemVisitor : ISqlItemVisitor<SqlItem>
    {
        public virtual SqlItem VisitExpr( SqlItem e )
        {
            return e.Accept( this );
        }

        protected List<ISqlItem> VisitItems( IEnumerable<ISqlItem> components, ISqlItem prefixToKeep = null, ISqlItem suffixToKeep = null )
        {
            List<ISqlItem> modified = null;
            int i = 0;
            foreach( var a in components )
            {
                var ce = a as SqlItem;
                if( ce != null )
                {
                    SqlItem ve = VisitExpr( ce );
                    if( !ReferenceEquals( ce, ve ) )
                    {
                        if( modified == null )
                        {
                            modified = new List<ISqlItem>( i+1 );
                            if( prefixToKeep != null ) modified.Add( prefixToKeep );
                            if( i > 0 )
                            {
                                using( var oldE = components.GetEnumerator() )
                                {
                                    int j = i;
                                    while( --j > 0 ) 
                                    {
                                        oldE.MoveNext();
                                        modified.Add( oldE.Current );
                                    }
                                }
                            }
                        }
                        modified[i] = ve;
                    }
                }
                ++i;
            }
            if( modified != null && suffixToKeep != null ) modified.Add( suffixToKeep );
            return modified;
        }

        public virtual SqlItem Visit( SqlExprUnmodeledItems e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprRawItemList e )
        {
            List<ISqlItem> modified = VisitItems( e.ItemsWithoutParenthesis, e.Opener, e.Closer );
            if( modified == null ) return e;
            return new SqlExprRawItemList( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprKoCall e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprKoCall( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprStIf e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprStIf( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprStBeginTran e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprStBeginTran( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprStatementList e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprStatementList( (SqlExprBaseSt[])modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprStBlock e )
        {
            SqlExprStatementList vB = (SqlExprStatementList)VisitExpr( e.Body );
            if( ReferenceEquals( vB, e.Body ) ) return e;
            return new SqlExprStBlock( e.Begin, vB, e.End, e.StatementTerminator );
        }

        public virtual SqlItem Visit( SqlExprStTryCatch e )
        {
            SqlExprStatementList vB = (SqlExprStatementList)VisitExpr( e.Body );
            SqlExprStatementList vC = (SqlExprStatementList)VisitExpr( e.BodyCatch );
            if( ReferenceEquals( vB, e.Body ) && ReferenceEquals( vC, e.BodyCatch) ) return e;
            return new SqlExprStTryCatch( e.BeginTry, vB, e.EndTryBeginCatch, vC, e.EndCatch, e.StatementTerminator );
        }

        public virtual SqlItem Visit( SqlExprStUnmodeled e )
        {
            SqlExpr vC = (SqlExpr)VisitExpr( e.Content );
            if( ReferenceEquals( vC, e.Content ) ) return e;
            return new SqlExprStUnmodeled( vC, e.StatementTerminator );
        }

        public virtual SqlItem Visit( SqlExprStStoredProc e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprStStoredProc( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprStMonoStatement e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprStLabelDef e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprStEmpty e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprStView e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprStView( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprColumnList e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprColumnList( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlNoExprExecuteAs e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlNoExprExecuteAs( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprCommaList e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprCommaList( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprIdentifier e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprMultiIdentifier e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprTerminal e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprLiteral e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprNull e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprUnaryOperator e )
        {
            SqlExpr vE = (SqlExpr)VisitExpr( e.Expression );
            if( ReferenceEquals( vE, e.Expression ) ) return e;
            if( vE == null ) return null;
            return new SqlExprUnaryOperator( e.Operator, vE );
        }

        public virtual SqlItem Visit( SqlExprTypeDecl e )
        {
            SqlItem vE = VisitExpr( (SqlItem)e.ActualType );
            if( ReferenceEquals( vE, e.ActualType ) ) return e;
            return new SqlExprTypeDecl( (ISqlExprUnifiedTypeDecl)vE );
        }

        public virtual SqlItem Visit( SqlExprTypeDeclDecimal e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprTypeDeclDateAndTime e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprTypeDeclSimple e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprTypeDeclWithSize e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprTypeDeclUserDefined e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprTypedIdentifier e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprParameter e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprParameter( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprParameterDefaultValue e )
        {
            return e;
        }

        public virtual SqlItem Visit( SqlExprParameterList e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprParameterList( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprAssign e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprAssign( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprBinaryOperator e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprBinaryOperator( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprIsNull e )
        {
            SqlExpr vE = (SqlExpr)VisitExpr( e.Left );
            if( ReferenceEquals( vE, e ) ) return e;
            return new SqlExprIsNull( vE, e.IsToken, e.NotToken, e.NullToken );
        }

        public virtual SqlItem Visit( SqlExprLike e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprLike( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprBetween e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprBetween( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprIn e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprIn( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprCase e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprCase( modified.ToArray() );
        }

        public virtual SqlItem Visit( SqlExprCaseWhenSelector e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SqlExprCaseWhenSelector( modified.ToArray() );
        }


        #region Select

        public virtual SqlItem Visit( SelectQuery e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectQuery( modified.ToArray() );
        }
        
        public virtual SqlItem Visit( SelectSpecification e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectSpecification( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectColumn e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectColumn( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectColumnList e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectColumnList( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectHeader e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectHeader( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectInto e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectInto( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectFrom e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectFrom( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectWhere e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectWhere( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectGroupBy e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectGroupBy( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectCombineOperator e )
        {
            ISelectSpecification lE = (ISelectSpecification)VisitExpr( e.Left );
            ISelectSpecification rE = (ISelectSpecification)VisitExpr( e.Right );
            if( ReferenceEquals( lE, e.Left ) && ReferenceEquals( rE, e.Right ) ) return e;
            if( lE == null ) return (SqlItem)lE;
            if( rE == null ) return (SqlItem)rE;
            return new SelectCombineOperator( SqlItem.CreateArray( e.Opener, lE, e.Operator, rE, e.Closer ) );
        }

        public virtual SqlItem Visit( SelectOrderBy e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectOrderBy( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectOrderByColumnList e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectOrderByColumnList( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectOrderByColumn e )
        {
            SqlExpr modified = (SqlExpr)VisitExpr( e.Definition );
            if( modified == null ) return e;
            if( modified != e.Definition ) return new SelectOrderByColumn( modified, e.AscOrDescToken );
            return e;
        }

        public virtual SqlItem Visit( SelectOrderByOffset e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectOrderByOffset( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectFor e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectFor( modified.ToArray() );
        }

        public virtual SqlItem Visit( SelectOption e )
        {
            List<ISqlItem> modified = VisitItems( e.Components );
            if( modified == null ) return e;
            return new SelectOption( modified.ToArray() );
        }

        #endregion

    }
}
