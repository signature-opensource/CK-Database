using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprVisitor : IExprVisitor<SqlExpr>
    {
        public virtual SqlExpr VisitExpr( SqlExpr e )
        {
            return e.Accept( this );
        }

        protected List<IAbstractExpr> VisitExprComponents( IEnumerable<IAbstractExpr> components, IAbstractExpr prefixToKeep = null, IAbstractExpr suffixToKeep = null )
        {
            List<IAbstractExpr> modified = null;
            int i = 0;
            foreach( var a in components )
            {
                SqlExpr ce = a as SqlExpr;
                if( ce != null )
                {
                    SqlExpr ve = VisitExpr( ce );
                    if( !ReferenceEquals( ce, ve ) )
                    {
                        if( modified == null )
                        {
                            modified = new List<IAbstractExpr>( i+1 );
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

        public virtual SqlExpr Visit( SqlExprUnmodeledTokens e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprGenericBlock e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.ComponentsWithoutParenthesis, e.Opener, e.Closer );
            if( modified == null ) return e;
            return new SqlExprGenericBlock( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprKoCall e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprKoCall( e, modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprStIf e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprStIf( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprStatementList e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprStatementList( (SqlExprBaseSt[])modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprStBlock e )
        {
            SqlExpr vB = VisitExpr( e.Body );
            if( ReferenceEquals( vB, e.Body ) ) return e;
            return new SqlExprStBlock( e.Begin, (SqlExprStatementList)vB, e.End, e.StatementTerminator );
        }

        public virtual SqlExpr Visit( SqlExprStUnmodeled e )
        {
            SqlExpr vC = VisitExpr( e.Content );
            if( ReferenceEquals( vC, e.Content ) ) return e;
            return new SqlExprStUnmodeled( e.Identifier, (SqlExprList)vC, e.StatementTerminator );
        }

        public virtual SqlExpr Visit( SqlExprStStoredProc e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprStStoredProc( e, modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprStMonoStatement e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprStLabelDef e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprStEmpty e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprStView e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprStView( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprColumnList e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprColumnList( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprList e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprList( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprIdentifier e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprMultiIdentifier e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprTerminal e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprLiteral e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprNull e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprUnaryOperator e )
        {
            SqlExpr vE = VisitExpr( e.Expression );
            if( ReferenceEquals( vE, e.Expression ) ) return e;
            return new SqlExprUnaryOperator( e.Operator, vE );
        }

        public virtual SqlExpr Visit( SqlExprTypeDecl e )
        {
            SqlExpr vE = VisitExpr( (SqlExpr)e.ActualType );
            if( ReferenceEquals( vE, e.ActualType ) ) return e;
            return new SqlExprTypeDecl( (ISqlExprUnifiedTypeDecl)vE );
        }

        public virtual SqlExpr Visit( SqlExprTypeDeclDecimal e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprTypeDeclDateAndTime e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprTypeDeclSimple e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprTypeDeclWithSize e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprTypeDeclUserDefined e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprTypedIdentifier e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprParameter e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprParameter( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprParameterDefaultValue e )
        {
            return e;
        }

        public virtual SqlExpr Visit( SqlExprParameterList e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprParameterList( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprAssign e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprAssign( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprBinaryOperator e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprBinaryOperator( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprIsNull e )
        {
            SqlExpr vE = VisitExpr( e.Left );
            if( ReferenceEquals( vE, e ) ) return e;
            return new SqlExprIsNull( vE, e.IsToken, e.NotToken, e.NullToken );
        }

        public virtual SqlExpr Visit( SqlExprLike e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprLike( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprBetween e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprBetween( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprIn e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprIn( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprSelectSpec e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprSelectSpec( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprSelectColumn e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprSelectColumn( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprSelectColumnList e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprSelectColumnList( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprSelectHeader e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprSelectHeader( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprSelectInto e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprSelectInto( modified.ToArray() );
        }        

        public virtual SqlExpr Visit( SqlExprSelectFrom e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprSelectFrom( modified.ToArray() );
        }        

        public virtual SqlExpr Visit( SqlExprSelectWhere e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprSelectWhere( modified.ToArray() );
        }

        public virtual SqlExpr Visit( SqlExprSelectGroupBy e )
        {
            List<IAbstractExpr> modified = VisitExprComponents( e.Components );
            if( modified == null ) return e;
            return new SqlExprSelectGroupBy( modified.ToArray() );
        }        


    }
}
