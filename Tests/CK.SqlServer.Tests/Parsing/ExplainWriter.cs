using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Tests.Parsing
{
    public class ExplainWriter : SqlExprVisitor
    {
        readonly StringBuilder Out;

        public ExplainWriter()
        {
            Out = new StringBuilder();
        }

        public static string Write( SqlItem e )
        {
            ExplainWriter w = new ExplainWriter();
            w.VisitExpr( e );
            return w.Out.ToString();
        }

        public override SqlItem Visit( SqlExprAssign e )
        {
            Out.Append( '[' );
            WriteIdentifier( e.Identifier );
            Out.Append( e.AssignToken.ToString() );
            VisitExpr( e.Right );           
            Out.Append( ']' );
            return e;
        }

        public override SqlItem Visit( SqlExprBinaryOperator e )
        {
            Out.Append( '[' );
            VisitExpr( e.Left );
            Out.Append( e.Middle.ToString().ToLowerInvariant() );
            VisitExpr( e.Right );
            Out.Append( ']' );
            return e;
        }

        public override SqlItem Visit( SqlExprIdentifier e )
        {
            WriteIdentifier( e );
            return e;
        }

        public override SqlItem Visit( SqlExprMultiIdentifier e )
        {
            WriteIdentifier( e );
            return e;
        }

        void WriteIdentifier( ISqlIdentifier id )
        {
            id.TokensWithoutParenthesis.WriteTokensWithoutTrivias( String.Empty, Out );
            //Out.Append( String.Join( ".", id.Select( n => n.Name ) ) );
        }

        public override SqlItem Visit( SqlExprStIf e )
        {
            Out.Append( "if[" );
            VisitExpr( e.Condition );
            Out.Append( "]then[" );
            VisitExpr( e.ThenStatement );
            Out.Append( ']' );
            if( e.HasElse )
            {
                Out.Append( "else[" );
                VisitExpr( e.ElseStatement );
                Out.Append( ']' );
            }
            return e;
        }

        public override SqlItem Visit( SqlExprStUnmodeled e )
        {
            Out.Append( '<' );
            VisitExpr( e.Content );
            Out.Append( '>' );
            return e;
        }

        public override SqlItem Visit( SqlExprStEmpty e )
        {
            Out.Append( "<empty statement>" );
            return e;
        }

        public override SqlItem Visit( SqlExprLiteral e )
        {
            Out.Append( e.Token.LiteralValue );
            return e;
        }

        public override SqlItem Visit( SqlExprNull e )
        {
            Out.Append( "null" );
            return e;
        }

        public override SqlItem Visit( SqlExprTerminal e )
        {
            Out.Append( SqlTokenizer.Explain( e.Token.TokenType ) );
            return e;
        }

        public override SqlItem Visit( SqlExprUnaryOperator e )
        {
            Out.Append( e.Operator.ToString().ToLowerInvariant() ).Append( '[' );
            VisitExpr( e.Expression );
            Out.Append( ']' );
            return e;
        }

        public override SqlItem Visit( SqlExprRawItemList e )
        {
            Out.Append( "¤{" );
            bool one = false;
            foreach( var item in e.ItemsWithoutParenthesis )
            {
                if( one ) Out.Append( '-' );
                one = true;
                SqlExpr iE = item as SqlExpr;
                if( iE != null )
                {
                    VisitExpr( iE );
                }
                else item.Tokens.WriteTokensWithoutTrivias( "-", Out );
            }
            Out.Append( "}¤" );
            return e;
        }

        public override SqlItem Visit( SqlExprCommaList e )
        {
            Out.Append( '{' );
            bool one = false;
            foreach( var item in e )
            {
                if( one ) Out.Append( ',' );
                one = true;
                VisitExpr( item );
            }
            Out.Append( '}' );
            return e;
        }

        public override SqlItem Visit( SqlExprIsNull e )
        {
            Out.Append( e.IsNotNull ? "IsNotNull(" : "IsNull(" );
            VisitExpr( e.Left );
            Out.Append( ')' );
            return e;
        }

        public override SqlItem Visit( SqlExprBetween e )
        {
            Out.Append( e.IsNotBetween ? "NotBetween(" : "Between(" );
            VisitExpr( e.Left );
            Out.Append( ',' );
            VisitExpr( e.Start );
            Out.Append( ',' );
            VisitExpr( e.Stop );
            Out.Append( ')' );
            return e;
        }

        public override SqlItem Visit( SqlExprCase e )
        {
            Out.Append( "case" );
            if( e.IsSimpleCase )
            {
                Out.Append( '(' );
                VisitExpr( e.Expression );
                Out.Append( ')' );
            }
            VisitExpr( e.WhenSelector );
            if( e.HasElse )
            {
                Out.Append( ':' );
                VisitExpr( e.ElseExpression );
            }
            return e;
        }

        public override SqlItem Visit( SqlExprCaseWhenSelector e )
        {
            for( int i = 0; i < e.Count; ++i )
            {
                Out.Append( ':' );
                VisitExpr( e.ExpressionAt( i ) );
                Out.Append( "=>" );
                VisitExpr( e.ExpressionValueAt( i ) );
            }
            return e;
        }

        public override SqlItem Visit( SqlExprLike e )
        {
            Out.Append( e.IsNotLike ? "NotLike(" : "Like(" );
            VisitExpr( e.Left );
            Out.Append( ',' );
            VisitExpr( e.Pattern );
            if( e.HasEscape )
            {
                Out.Append( ',' );
                Out.Append( e.EscapeChar.LiteralValue );
            } 
            Out.Append( ')' );
            return e;
        }

        public override SqlItem Visit( SqlExprIn e )
        {
            Out.Append( e.IsNotIn ? "NotIn(" : "In(" );
            VisitExpr( e.Left );
            Out.Append( '∈' );
            VisitExpr( e.Values );
            Out.Append( ')' );
            return e;
        }

        public override SqlItem Visit( SqlExprKoCall e )
        {
            Out.Append( "call:" );
            VisitExpr( e.FunName );
            Out.Append( '(' );
            bool already = false;
            foreach( SqlExpr b in e.Parameters )
            {
                if( already ) Out.Append( ',' );
                VisitExpr( b );
                already = true;
            }
            Out.Append( ')' );
            return e;
        }

        public override SqlItem Visit( SelectSpecification e )
        {
            Out.Append( '[' );
            VisitExpr( e.Header );
            Out.Append( "-" );
            VisitExpr( e.Columns );
            if( e.IntoClause != null ) VisitExpr( e.IntoClause );
            if( e.FromClause != null ) VisitExpr( e.FromClause );
            if( e.WhereClause != null ) VisitExpr( e.WhereClause );
            if( e.GroupByClause != null ) VisitExpr( e.GroupByClause );
            Out.Append( ']' );
            return e;
        }

        public override SqlItem Visit( SelectHeader e )
        {
            e.Tokens.WriteTokensWithoutTrivias( "-", Out );
            return e;
        }

        public override SqlItem Visit( SelectColumnList e )
        {
            Out.Append( "(" );
            bool atLeastOne = false;
            foreach( SelectColumn c in e )
            {
                if( atLeastOne ) Out.Append( "," );
                else atLeastOne = true;
                VisitExpr( c );
            }
            Out.Append( ")" );
            return e;
        }

        public override SqlItem Visit( SelectColumn e )
        {
            if( e.ColumnName != null )
            {
                WriteIdentifier( e.ColumnName );
                Out.Append( '-' );
                e.AsOrEqual.WriteWithoutTrivias( Out );
                Out.Append( '-' );
            }
            VisitExpr( e.Definition );
            return e;
        }

        public override SqlItem Visit( SelectInto e )
        {
            Out.Append( "-into[" );
            WriteIdentifier( e.TableName );
            Out.Append( "]" );
            return e;
        }

        public override SqlItem Visit( SelectFrom e )
        {
            Out.Append( "-from[" );
            VisitExpr( e.Content );
            Out.Append( "]" );
            return e;
        }

        public override SqlItem Visit( SelectWhere e )
        {
            Out.Append( "-where[" );
            VisitExpr( e.Expression );
            Out.Append( "]" );
            return e;
        }

        public override SqlItem Visit( SelectGroupBy e )
        {
            Out.Append( "-groupBy[" );
            VisitExpr( e.GroupExpression );
            Out.Append( "]" );
            if( e.HavingExpression != null )
            {
                Out.Append( "-having[" );
                VisitExpr( e.HavingExpression );
                Out.Append( "]" );
            }
            return e;
        }

        public override SqlItem Visit( SelectOrderBy e )
        {
            Out.Append( "OrderBy(" );
            VisitExpr( e.SelectExpr );
            Out.Append( "," );
            VisitExpr( e.OrderByExpression );
            Out.Append( ")" );
            return e;
        }

        public override SqlItem Visit( SelectFor e )
        {
            Out.Append( "For(" );
            VisitExpr( e.SelectExpr );
            Out.Append( "," );
            VisitExpr( e.ForExpression );
            Out.Append( ")" );
            return e;
        }

        public override SqlItem Visit( SelectCombineOperator e )
        {
            Out.Append( '[' );
            VisitExpr( e.Left );
            e.Operator.Tokens.WriteTokensWithoutTrivias( "-", Out );
            VisitExpr( e.Right );
            Out.Append( ']' );
            return e;
        }

        public override SqlItem Visit( SelectOption e )
        {
            Out.Append( "-option[" );
            VisitExpr( e.Content );
            Out.Append( "]" );
            return e;
        }

    }
}
