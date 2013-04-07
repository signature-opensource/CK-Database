using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using CK.Core;
using System.Diagnostics;
using System.Globalization;

namespace CK.SqlServer
{
    public abstract class SqlExprAbstract : IAbstractExpr
    {
        /// <summary>
        /// Gets the components of this expression: it is a mix of <see cref="SqlToken"/> and <see cref="SqlExpr"/>.
        /// Never null nor empty since an expression covers at least one token.
        /// </summary>
        public abstract IEnumerable<IAbstractExpr> Components { get; }

        /// <summary>
        /// Gets the tokens that compose this expression.
        /// Never null nor empty: an expression covers at least one token.
        /// </summary>
        public virtual IEnumerable<SqlToken> Tokens  { get { return Flatten( Components ); } }

        /// <summary>
        /// Overriden to generate the representation of an expression as the result of the <see cref="Write"/> method.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            Write( b );
            return b.ToString();
        }

        /// <summary>
        /// Writing an expression is, by default, calling <see cref="SqlToken.Write"/> on each of its <see cref="Tokens"/>.
        /// </summary>
        /// <param name="b">StringBuilder to write into.</param>
        public void Write( StringBuilder b )
        {
            foreach( var t in Tokens ) t.Write( b );
        }

        static internal IAbstractExpr[] CreateArray( params IAbstractExpr[] e )
        {
            return e;
        }

        static internal SqlToken[] CreateArray( params SqlToken[] e )
        {
            return e;
        }

        static internal IAbstractExpr[] CreateArray( IEnumerable<IAbstractExpr> content, int contentLength, IAbstractExpr suffix )
        {
            Debug.Assert( content != null && suffix != null && contentLength <= content.Count() && contentLength >= 0 );
            var c = new IAbstractExpr[contentLength + 1];
            int i = 0;
            foreach( var e in content )
            {
                c[i++] = e;
                if( i == contentLength ) break;
            }
            c[contentLength] = suffix;
            return c;
        }

        static internal IAbstractExpr[] CreateArray( IAbstractExpr prefix, IEnumerable<IAbstractExpr> content, int skippedContent, int contentLength, IAbstractExpr suffix )
        {
            Debug.Assert( content != null && suffix != null && prefix != null 
                            && skippedContent >= 0 && contentLength >= 0 && skippedContent + contentLength <= content.Count() );
            var c = new IAbstractExpr[++contentLength + 1];
            c[0] = prefix;
            int i = 1;
            foreach( var e in content )
            {
                c[i++] = e;
                if( i == contentLength ) break;
            }
            c[contentLength] = suffix;
            return c;
        }

        static internal IAbstractExpr[] CreateArray( SqlTokenOpenPar openPar, IEnumerable<IAbstractExpr> content, int contentLength, SqlTokenClosePar closePar )
        {
            Debug.Assert( contentLength == 0 || !(content.First() is SqlExprMultiToken<SqlTokenOpenPar>) );
            return CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Create( openPar ), content, 0, contentLength, SqlExprMultiToken<SqlTokenClosePar>.Create( closePar ) );
        }

        static internal IAbstractExpr[] CreateArray( SqlExprMultiToken<SqlTokenOpenPar> prefix, IAbstractExpr[] enclosedComponents, SqlExprMultiToken<SqlTokenClosePar> suffix )
        {
            Debug.Assert( prefix != null && enclosedComponents != null && suffix != null );
            Debug.Assert( enclosedComponents.Length >= 2 );
            Debug.Assert( enclosedComponents[0] is SqlExprMultiToken<SqlTokenOpenPar> );
            Debug.Assert( enclosedComponents[enclosedComponents.Length-1] is SqlExprMultiToken<SqlTokenClosePar> );

            SqlExprMultiToken<SqlTokenOpenPar> existOpen = (SqlExprMultiToken<SqlTokenOpenPar>)enclosedComponents[0];
            SqlExprMultiToken<SqlTokenClosePar> existClose = (SqlExprMultiToken<SqlTokenClosePar>)enclosedComponents[enclosedComponents.Length-1];

            return CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Create( prefix, existOpen ), enclosedComponents, 1, enclosedComponents.Length, SqlExprMultiToken<SqlTokenClosePar>.Create( existClose, suffix ) );
        }


        static internal IEnumerable<SqlToken> Flatten( IEnumerable<IAbstractExpr> e )
        {
            foreach( var a in e )
            {
                SqlToken t = a as SqlToken;
                if( t != null ) yield return t;
                else foreach( var ta in Flatten( a.Tokens ) ) yield return ta;
            }
        }

        /// <summary>
        /// True if the <see cref="IAbstractExpr"/> is an unquoted identifier with a given name.
        /// </summary>
        /// <param name="t">Potential unquoted identifier.</param>
        /// <param name="name">Name of the unquoted identifier.</param>
        /// <returns>Whether the token is the named one.</returns>
        static public bool IsUnquotedIdentifier( IAbstractExpr t, string name )
        {
            return (t is SqlTokenIdentifier) && ((SqlTokenIdentifier)t).NameEquals( name );
        }

        /// <summary>
        /// True if the <see cref="IAbstractExpr"/> is an unquoted identifier with a given name or an alternate one.
        /// </summary>
        /// <param name="t">Potential unquoted identifier.</param>
        /// <param name="name">Name of the unquoted identifier.</param>
        /// <param name="altName">Alternate name of the unquoted identifier.</param>
        /// <returns>Whether the token the is named one.</returns>
        static public bool IsUnquotedIdentifier( IAbstractExpr t, string name, string altName )
        {
            return (t is SqlTokenIdentifier) && (((SqlTokenIdentifier)t).NameEquals( name )||((SqlTokenIdentifier)t).NameEquals( altName ));
        }

        /// <summary>
        /// True if the <see cref="IAbstractExpr"/> is a comma token.
        /// </summary>
        /// <param name="t">Potential comma token.</param>
        /// <returns>Whether the token is a comma or not.</returns>
        static public bool IsCommaSeparator( IAbstractExpr t )
        {
            return (t is SqlToken) && ((SqlToken)t).TokenType == SqlTokenType.Comma;
        }

        /// <summary>
        /// True if the <see cref="IAbstractExpr"/> is a dot token.
        /// </summary>
        /// <param name="t">Potential dot token.</param>
        /// <returns>Whether the token is a dot.</returns>
        static public bool IsDotSeparator( IAbstractExpr t )
        {
            return (t is SqlToken) && ((SqlToken)t).TokenType == SqlTokenType.Dot;
        }

        /// <summary>
        /// True if the <see cref="IAbstractExpr"/> is a <see cref="SqlTokenType.Dot">dot</see> or a <see cref="SqlTokenType.DoubleColons">double colon</see> token.
        /// </summary>
        /// <param name="t">Token to test.</param>
        /// <returns>Whether the token is a dot or double colon token.</returns>
        static public bool IsDotOrDoubleColonSeparator( IAbstractExpr t )
        {
            SqlToken tok = t as SqlToken;
            return tok != null && (tok.TokenType == SqlTokenType.Dot || tok.TokenType == SqlTokenType.DoubleColons);
        }

        /// <summary>
        /// True if the <see cref="IAbstractExpr"/> is a comma or a closing parenthesis or a ; token (this ends an element in a list).
        /// </summary>
        /// <param name="t">Potential comma, closing parenthesis or semicolon token.</param>
        /// <returns>Whether the token is a comma or a closing parenthesis or the statement terminator.</returns>
        static public bool IsCommaOrCloseParenthesisOrTerminator( IAbstractExpr t )
        {
            SqlToken token = t as SqlToken;
            return token != null && (token.TokenType == SqlTokenType.Comma || token.TokenType == SqlTokenType.ClosePar || token.TokenType == SqlTokenType.SemiColon);
        }

    }

}
