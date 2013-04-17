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

        public static string Write( SqlExpr e )
        {
            ExplainWriter w = new ExplainWriter();
            w.VisitExpr( e );
            return w.Out.ToString();
        }

        public override SqlExpr Visit( SqlExprAssign e )
        {
            Out.Append( '[' );
            WriteIdentifier( e.Identifier );
            Out.Append( SqlTokenizer.Explain( e.AssignToken.TokenType ) );
            VisitExpr( e.Right );           
            Out.Append( ']' );
            return e;
        }

        public override SqlExpr Visit( SqlExprBinaryOperator e )
        {
            Out.Append( '[' );
            VisitExpr( e.Left );
            Out.Append( SqlTokenizer.Explain( e.Middle.TokenType ) );
            VisitExpr( e.Right );
            Out.Append( ']' );
            return e;
        }

        public override SqlExpr Visit( SqlExprIdentifier e )
        {
            WriteIdentifier( e );
            return e;
        }

        public override SqlExpr Visit( SqlExprMultiIdentifier e )
        {
            WriteIdentifier( e );
            return e;
        }

        void WriteIdentifier( ISqlIdentifier id )
        {
            Out.Append( String.Join( ".", id.Select( n => n.Name ) ) );
        }

        public override SqlExpr Visit( SqlExprStIf e )
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

        public override SqlExpr Visit( SqlExprStUnmodeled e )
        {
            Out.Append( '<' ).Append( e.Identifier.Name );
            VisitExpr( e.Content );
            Out.Append( '>' );
            return e;
        }

        public override SqlExpr Visit( SqlExprStEmpty e )
        {
            Out.Append( "<empty statement>" );
            return e;
        }

        public override SqlExpr Visit( SqlExprLiteral e )
        {
            Out.Append( e.Token.LiteralValue );
            return e;
        }

        public override SqlExpr Visit( SqlExprNull e )
        {
            Out.Append( "null" );
            return e;
        }

        public override SqlExpr Visit( SqlExprTerminal e )
        {
            Out.Append( SqlTokenizer.Explain( e.Token.TokenType ) );
            return e;
        }

        public override SqlExpr Visit( SqlExprUnaryOperator e )
        {
            Out.Append( SqlTokenizer.Explain( e.Operator.TokenType ) ).Append( '[' );
            VisitExpr( e.Expression );
            Out.Append( ']' );
            return e;
        }

        public override SqlExpr Visit( SqlExprGenericBlock e )
        {
            Out.Append( "¤{" );
            bool one = false;
            foreach( var item in e.ComponentsWithoutParenthesis )
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

        public override SqlExpr Visit( SqlExprList e )
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

        public override SqlExpr Visit( SqlExprIsNull e )
        {
            Out.Append( e.IsNotNull ? "IsNotNull(" : "IsNull(" );
            VisitExpr( e.Left );
            Out.Append( ')' );
            return e;
        }

        public override SqlExpr Visit( SqlExprBetween e )
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

        public override SqlExpr Visit( SqlExprLike e )
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

        public override SqlExpr Visit( SqlExprIn e )
        {
            Out.Append( e.IsNotIn ? "NotIn(" : "In(" );
            VisitExpr( e.Left );
            Out.Append( '∈' );
            VisitExpr( e.Values );
            Out.Append( ')' );
            return e;
        }

        public override SqlExpr Visit( SqlExprKoCall e )
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

    }
}
