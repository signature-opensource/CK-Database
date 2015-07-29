using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Parser
{
    public static class ISqlItemExtension
    {
        /// <summary>
        /// Gets whether the <see cref="ISqlItem"/> is actually a <see cref="SqlToken"/> of a given type.
        /// </summary>
        /// <param name="this">Sql item.</param>
        /// <param name="type">The type of the token.</param>
        /// <returns>True on success.</returns>
        static public bool IsToken( this ISqlItem @this, SqlTokenType type )
        {
            SqlToken id = @this as SqlToken;
            return id != null && id.TokenType == type;
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is an unquoted identifier with a given name.
        /// Comparison is <see cref="StringComparison.OrdinalIgnoreCase"/>.
        /// </summary>
        /// <param name="t">Potential unquoted identifier.</param>
        /// <param name="name">Name of the unquoted identifier.</param>
        /// <returns>Whether the token is the named one.</returns>
        static public bool IsUnquotedIdentifier( this ISqlItem t, string name )
        {
            SqlTokenIdentifier id = t as SqlTokenIdentifier;
            return id != null && !id.IsQuoted && id.NameEquals( name );
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is an unquoted identifier with a given name or an alternate one.
        /// </summary>
        /// <param name="t">Potential unquoted identifier.</param>
        /// <param name="name">Name of the unquoted identifier.</param>
        /// <param name="altName">Alternate name of the unquoted identifier.</param>
        /// <returns>Whether the token the is named one.</returns>
        static public bool IsUnquotedIdentifier( this ISqlItem t, string name, string altName )
        {
            SqlTokenIdentifier id = t as SqlTokenIdentifier;
            return id != null && !id.IsQuoted && (id.NameEquals( name ) || id.NameEquals( altName ));
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a comma token.
        /// </summary>
        /// <param name="t">Potential comma token.</param>
        /// <returns>Whether the token is a comma or not.</returns>
        static public bool IsCommaSeparator( this ISqlItem t )
        {
            return (t is SqlToken) && ((SqlToken)t).TokenType == SqlTokenType.Comma;
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a dot token.
        /// </summary>
        /// <param name="t">Potential dot token.</param>
        /// <returns>Whether the token is a dot.</returns>
        static public bool IsDotSeparator( this ISqlItem t )
        {
            return (t is SqlToken) && ((SqlToken)t).TokenType == SqlTokenType.Dot;
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a <see cref="SqlTokenType.Dot">dot</see> or a <see cref="SqlTokenType.DoubleColons">double colon</see> token.
        /// </summary>
        /// <param name="t">Token to test.</param>
        /// <returns>Whether the token is a dot or double colon token.</returns>
        static public bool IsDotOrDoubleColonSeparator( this ISqlItem t )
        {
            SqlToken token = t as SqlToken;
            return token != null && (token.TokenType == SqlTokenType.Dot || token.TokenType == SqlTokenType.DoubleColons);
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a comma or a closing parenthesis or a ; token (this ends an element in a list).
        /// </summary>
        /// <param name="t">Potential comma, closing parenthesis or semicolon token.</param>
        /// <returns>Whether the token is a comma or a closing parenthesis or the statement terminator.</returns>
        static public bool IsCommaOrCloseParenthesisOrTerminator( this ISqlItem t )
        {
            SqlToken token = t as SqlToken;
            return token != null && (token.TokenType == SqlTokenType.EndOfInput || token.TokenType == SqlTokenType.Comma || token.TokenType == SqlTokenType.ClosePar || token.TokenType == SqlTokenType.SemiColon);
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a closing parenthesis or a ; token (this ends an element in a list).
        /// </summary>
        /// <param name="t">Closing parenthesis or semicolon token.</param>
        /// <returns>Whether the token is closing parenthesis or the statement terminator.</returns>
        static public bool IsCloseParenthesisOrTerminator( this ISqlItem t )
        {
            SqlToken token = t as SqlToken;
            return token != null && (token.TokenType == SqlTokenType.ClosePar || token.TokenType == SqlTokenType.SemiColon);
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a closing parenthesis, a terminator ; token or a <see cref="SqlTokenType.IdentifierReservedStatement"/>.
        /// </summary>
        /// <param name="t">Closing parenthesis or semicolon token.</param>
        /// <returns>Whether the token is closing parenthesis or the statement terminator.</returns>
        static public bool IsCloseParenthesisOrTerminatorOrPossibleStartStatement( this ISqlItem t )
        {
            SqlToken token = t as SqlToken;
            return token != null
                && (token.TokenType == SqlTokenType.ClosePar
                    || token.TokenType == SqlTokenType.SemiColon
                    || (token.TokenType & SqlTokenType.IdentifierTypeMask) == SqlTokenType.IdentifierStandardStatement
                    || (token.TokenType & SqlTokenType.IdentifierTypeMask) == SqlTokenType.IdentifierReservedStatement);
        }


    }
}
