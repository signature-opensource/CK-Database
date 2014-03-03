using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser.Tests
{
    public class ExplainWriter : SqlItemVisitor
    {
        readonly StringBuilder Out;

        public ExplainWriter()
        {
            Out = new StringBuilder();
        }

        public static string Write( SqlItem e )
        {
            ExplainWriter w = new ExplainWriter();
            w.VisitItem( e );
            return w.Out.ToString();
        }

        public override SqlItem Visit( SqlExprAssign e )
        {
            Out.Append( '[' );
            WriteIdentifier( e.Identifier );
            Out.Append( e.AssignTok.ToString() );
            VisitItem( e.Right );           
            Out.Append( ']' );
            return e;
        }

        public override SqlItem Visit( SqlExprBinaryOperator e )
        {
            Out.Append( '[' );
            VisitItem( e.Left );
            Out.Append( e.Middle.ToString().ToLowerInvariant() );
            VisitItem( e.Right );
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
            VisitItem( e.Condition );
            Out.Append( "]then[" );
            VisitItem( e.ThenStatement );
            Out.Append( ']' );
            if( e.HasElse )
            {
                Out.Append( "else[" );
                VisitItem( e.ElseStatement );
                Out.Append( ']' );
            }
            return e;
        }

        public override SqlItem Visit( SqlExprStUnmodeled e )
        {
            Out.Append( '<' );
            VisitItem( e.Content );
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
            VisitItem( e.Expression );
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
                SqlItem iE = item as SqlItem;
                if( iE != null )
                {
                    VisitItem( iE );
                }
                else item.Tokens.WriteTokensWithoutTrivias( "-", Out );
            }
            Out.Append( "}¤" );
            return e;
        }

        public override SqlItem Visit( SqlExprTypeDecl e )
        {
            e.Tokens.WriteTokensWithoutTrivias( "-", Out );
            return e;
        }

        public override SqlItem Visit( SqlExprCast e )
        {
            Out.Append( '(' );
            Visit( e.Type );
            Out.Append( ')' );
            Out.Append( '[' );
            VisitItem( e.Expression );
            Out.Append( ']' );
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
                VisitItem( item );
            }
            Out.Append( '}' );
            return e;
        }

        public override SqlItem Visit( SqlExprIsNull e )
        {
            Out.Append( e.IsNotNull ? "IsNotNull(" : "IsNull(" );
            VisitItem( e.Left );
            Out.Append( ')' );
            return e;
        }

        public override SqlItem Visit( SqlExprBetween e )
        {
            Out.Append( e.IsNotBetween ? "NotBetween(" : "Between(" );
            VisitItem( e.Left );
            Out.Append( ',' );
            VisitItem( e.Start );
            Out.Append( ',' );
            VisitItem( e.Stop );
            Out.Append( ')' );
            return e;
        }

        public override SqlItem Visit( SqlExprCase e )
        {
            Out.Append( "case" );
            if( e.IsSimpleCase )
            {
                Out.Append( '(' );
                VisitItem( e.Expression );
                Out.Append( ')' );
            }
            VisitItem( e.WhenSelector );
            if( e.HasElse )
            {
                Out.Append( ':' );
                VisitItem( e.ElseExpression );
            }
            return e;
        }

        public override SqlItem Visit( SqlExprCaseWhenSelector e )
        {
            for( int i = 0; i < e.Count; ++i )
            {
                Out.Append( ':' );
                VisitItem( e.ExpressionAt( i ) );
                Out.Append( "=>" );
                VisitItem( e.ExpressionValueAt( i ) );
            }
            return e;
        }

        public override SqlItem Visit( SqlExprLike e )
        {
            Out.Append( e.IsNotLike ? "NotLike(" : "Like(" );
            VisitItem( e.Left );
            Out.Append( ',' );
            VisitItem( e.Pattern );
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
            VisitItem( e.Left );
            Out.Append( '∈' );
            VisitItem( e.Values );
            Out.Append( ')' );
            return e;
        }

        public override SqlItem Visit( SqlExprKoCall e )
        {
            Out.Append( "call:" );
            VisitItem( e.FunName );
            Out.Append( '(' );
            bool already = false;
            foreach( SqlExpr b in e.Parameters )
            {
                if( already ) Out.Append( ',' );
                VisitItem( b );
                already = true;
            }
            Out.Append( ')' );
            if( e.OverClause != null ) Visit( e.OverClause );
            return e;
        }

        public override SqlItem Visit( SqlNoExprOverClause e )
        {
            Out.Append( "OVER[" );
            VisitItem( e.OverExpression );
            Out.Append( ']' );
            return e;
        }
        
        public override SqlItem Visit( SelectSpecification e )
        {
            Out.Append( '[' );
            VisitItem( e.Header );
            Out.Append( "-" );
            VisitItem( e.Columns );
            if( e.IntoClause != null ) VisitItem( e.IntoClause );
            if( e.FromClause != null ) VisitItem( e.FromClause );
            if( e.WhereClause != null ) VisitItem( e.WhereClause );
            if( e.GroupByClause != null ) VisitItem( e.GroupByClause );
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
                VisitItem( c );
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
                e.AsOrEqualTok.WriteWithoutTrivias( Out );
                Out.Append( '-' );
            }
            VisitItem( e.Definition );
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
            VisitItem( e.Content );
            Out.Append( "]" );
            return e;
        }

        public override SqlItem Visit( SelectWhere e )
        {
            Out.Append( "-where[" );
            VisitItem( e.Expression );
            Out.Append( "]" );
            return e;
        }

        public override SqlItem Visit( SelectGroupBy e )
        {
            Out.Append( "-groupBy[" );
            VisitItem( e.GroupExpression );
            Out.Append( "]" );
            if( e.HavingExpression != null )
            {
                Out.Append( "-having[" );
                VisitItem( e.HavingExpression );
                Out.Append( "]" );
            }
            return e;
        }

        public override SqlItem Visit( SelectOrderBy e )
        {
            Out.Append( "OrderBy(" );
            VisitItem( e.SelectExpr );
            Out.Append( "," );
            VisitItem( e.OrderByColumns );
            if( e.OffsetClause != null )
            {
                Out.Append( "," );
                VisitItem( e.OffsetClause );
            }
            Out.Append( ")" );
            return e;
        }

        public override SqlItem Visit( SelectOrderByColumnList e )
        {
            Out.Append( "(" );
            bool atLeastOne = false;
            foreach( SelectOrderByColumn c in e )
            {
                if( atLeastOne ) Out.Append( "," );
                else atLeastOne = true;
                VisitItem( c );
            }
            Out.Append( ")" );
            return e;
        }

        public override SqlItem Visit( SelectOrderByColumn e )
        {
            VisitItem( e.Definition );
            if( e.AscOrDescToken != null )
            {
                Out.Append( "-" );
                Out.Append( e.AscOrDescToken.Name );
            }
            return e;
        }

        public override SqlItem Visit( SelectOrderByOffset e )
        {
            Out.Append( "offset:" );
            VisitItem( e.OffsetExpression );
            if( e.HasFetchClause )
            {
                Out.Append( ",fetch:" );
                VisitItem( e.FetchExpression );
            }
            return e;
        }


        public override SqlItem Visit( SelectFor e )
        {
            Out.Append( "For(" );
            VisitItem( e.SelectExpr );
            Out.Append( "," );
            VisitItem( e.ForExpression );
            Out.Append( ")" );
            return e;
        }

        public override SqlItem Visit( SelectCombineOperator e )
        {
            Out.Append( '[' );
            VisitItem( e.Left );
            e.Operator.Tokens.WriteTokensWithoutTrivias( "-", Out );
            VisitItem( e.Right );
            Out.Append( ']' );
            return e;
        }

        public override SqlItem Visit( SelectOption e )
        {
            Out.Append( "-option[" );
            VisitItem( e.Content );
            Out.Append( "]" );
            return e;
        }

    }
}
