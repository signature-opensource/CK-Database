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
    /// <summary>
    /// Base class for (non comment) tokens. This is an immutable object that carries its optional leading and trailing <see cref="SqlTrivia"/>.
    /// </summary>
    public abstract class SqlToken : ISqlItem
    {
        class EmptyToken : SqlToken
        {
            internal EmptyToken() : base() { }
            protected override void DoWrite( StringBuilder b ) { }
            public override string ToString() { return String.Empty; }
        }

        /// <summary>
        /// Empty token has a <see cref="SqlToken.TokenType"/> of <see cref="SqlTokenType.None"/> and empty leading and trailing trivias.
        /// </summary>
        public static readonly SqlToken Empty = new EmptyToken();

        /// <summary>
        /// Private empty ctor for the EmptyToken singleton.
        /// </summary>
        SqlToken()
        {
            TokenType = SqlTokenType.None;
            LeadingTrivia = TrailingTrivia = CKReadOnlyListEmpty<SqlTrivia>.Empty;
        }

        /// <summary>
        /// Initializes a new <see cref="SqlToken"/>. <paramref name="tokenType"/> must be strictly positive (not an error) and not <see cref="SqlTokenType.IsComment"/>.
        /// When null, trivias are safely sets to an empty readonly list of <see cref="SqlTrivia"/>.
        /// </summary>
        /// <param name="tokenType">Type of the token.</param>
        /// <param name="leadingTrivia">Leading trivias if any.</param>
        /// <param name="trailingTrivia">Trailing trivias if any.</param>
        public SqlToken( SqlTokenType tokenType, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
        {
            if( tokenType > 0 && (tokenType & (SqlTokenType.TokenDiscriminatorMask & ~SqlTokenType.IsComment)) == 0 ) throw new ArgumentException( "Invalid token type." );
            
            TokenType = tokenType;
            LeadingTrivia = leadingTrivia ?? CKReadOnlyListEmpty<SqlTrivia>.Empty;
            TrailingTrivia = trailingTrivia ?? CKReadOnlyListEmpty<SqlTrivia>.Empty;
        }

        /// <summary>
        /// Token type. It is necessarily positive (not an error). Only <see cref="Empty"/> has <see cref="SqlTokenType.None"/> type.
        /// </summary>
        public readonly SqlTokenType TokenType;

        /// <summary>
        /// Leading <see cref="SqlTrivia"/>. Never null but can be empty.
        /// </summary>
        public readonly IReadOnlyList<SqlTrivia> LeadingTrivia;

        /// <summary>
        /// Trailing <see cref="SqlTrivia"/>. Never null but can be empty.
        /// </summary>
        public readonly IReadOnlyList<SqlTrivia> TrailingTrivia;

        /// <summary>
        /// Writes the token with its <see cref="LeadingTrivia"/> and <see cref="TrailingTrivia"/>.
        /// </summary>
        /// <param name="b">The <see cref="StringBuilder"/> to write to.</param>
        public void Write( StringBuilder b )
        {
            foreach( var t in LeadingTrivia ) t.Write( b );
            DoWrite( b );
            foreach( var t in TrailingTrivia ) t.Write( b );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b">The <see cref="StringBuilder"/> to write to.</param>
        public void WriteWithoutTrivias( StringBuilder b )
        {
            DoWrite( b );
        }

        /// <summary>
        /// When implemented by concrete specialization, this must write the token itself.
        /// </summary>
        /// <param name="b">The <see cref="StringBuilder"/> to write to.</param>
        abstract protected void DoWrite( StringBuilder b );

        /// <summary>
        /// Overriden to return the result of <see cref="WriteWithoutTrivias"/>.
        /// This should not be overriden anymore.
        /// </summary>
        /// <returns>The mere token.</returns>
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            DoWrite( b );
            return b.ToString();
        }

        IEnumerable<SqlToken> ISqlItem.Tokens
        {
            get { return new CKReadOnlyListMono<SqlToken>( this ); }
        }

        SqlToken ISqlItem.LastOrEmptyToken { get { return this; } }

        SqlToken ISqlItem.FirstOrEmptyToken { get { return this; } }

        /// <summary>
        /// Empty parenthesis opener.
        /// </summary>
        static public readonly SqlExprMultiToken<SqlTokenOpenPar> EmptyOpenPar = SqlExprMultiToken<SqlTokenOpenPar>.Empty;

        /// <summary>
        /// Empty parenthesis closer.
        /// </summary>
        static public readonly SqlExprMultiToken<SqlTokenClosePar> EmptyClosePar = SqlExprMultiToken<SqlTokenClosePar>.Empty;

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is an unquoted identifier with a given name.
        /// </summary>
        /// <param name="t">Potential unquoted identifier.</param>
        /// <param name="name">Name of the unquoted identifier.</param>
        /// <returns>Whether the token is the named one.</returns>
        static public bool IsUnquotedIdentifier( ISqlItem t, string name )
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
        static public bool IsUnquotedIdentifier( ISqlItem t, string name, string altName )
        {
            SqlTokenIdentifier id = t as SqlTokenIdentifier;
            return id != null && !id.IsQuoted && (id.NameEquals( name ) || id.NameEquals( altName ));
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a comma token.
        /// </summary>
        /// <param name="t">Potential comma token.</param>
        /// <returns>Whether the token is a comma or not.</returns>
        static public bool IsCommaSeparator( ISqlItem t )
        {
            return (t is SqlToken) && ((SqlToken)t).TokenType == SqlTokenType.Comma;
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a dot token.
        /// </summary>
        /// <param name="t">Potential dot token.</param>
        /// <returns>Whether the token is a dot.</returns>
        static public bool IsDotSeparator( ISqlItem t )
        {
            return (t is SqlToken) && ((SqlToken)t).TokenType == SqlTokenType.Dot;
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a <see cref="SqlTokenType.Dot">dot</see> or a <see cref="SqlTokenType.DoubleColons">double colon</see> token.
        /// </summary>
        /// <param name="t">Token to test.</param>
        /// <returns>Whether the token is a dot or double colon token.</returns>
        static public bool IsDotOrDoubleColonSeparator( ISqlItem t )
        {
            SqlToken token = t as SqlToken;
            return token != null && (token.TokenType == SqlTokenType.Dot || token.TokenType == SqlTokenType.DoubleColons);
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a comma or a closing parenthesis or a ; token (this ends an element in a list).
        /// </summary>
        /// <param name="t">Potential comma, closing parenthesis or semicolon token.</param>
        /// <returns>Whether the token is a comma or a closing parenthesis or the statement terminator.</returns>
        static public bool IsCommaOrCloseParenthesisOrTerminator( ISqlItem t )
        {
            SqlToken token = t as SqlToken;
            return token != null && (token.TokenType == SqlTokenType.Comma || token.TokenType == SqlTokenType.ClosePar || token.TokenType == SqlTokenType.SemiColon);
        }

        /// <summary>
        /// True if the <see cref="ISqlItem"/> is a closing parenthesis or a ; token (this ends an element in a list).
        /// </summary>
        /// <param name="t">Closing parenthesis or semicolon token.</param>
        /// <returns>Whether the token is closing parenthesis or the statement terminator.</returns>
        static public bool IsCloseParenthesisOrTerminator( ISqlItem t )
        {
            SqlToken token = t as SqlToken;
            return token != null && (token.TokenType == SqlTokenType.ClosePar || token.TokenType == SqlTokenType.SemiColon);
        }

        /// <summary>
        /// True if the token is a @variable or a literal value ('string' or 0x5454 number for instance).
        /// </summary>
        /// <param name="t">Token to test.</param>
        /// <returns>True for a variable or a literal.</returns>
        static public bool IsVariableNameOrLiteral( SqlTokenType t )
        {
            return t == SqlTokenType.IdentifierVariable || (t & SqlTokenType.LitteralMask) != 0;
        }

        internal static bool IsIdentifierStartChar( int c )
        {
            return c == '@' || c == '#' || c == '_' || Char.IsLetter( (char)c );
        }

        internal static bool IsIdentifierChar( int c )
        {
            return IsIdentifierStartChar( c ) || Char.IsDigit( (char)c );
        }

        /// <summary>
        /// Tests whether an identifier must be quoted (it is empty, starts with @ or contains a character that is not valid).
        /// </summary>
        /// <param name="identifier">Identifier to test.</param>
        /// <returns>True if the identifier can be used without surrounding quotes.</returns>
        static public bool IsQuoteRequired( string identifier )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( identifier.Length > 0 )
            {
                char c = identifier[0];
                if( c != '@' && IsIdentifierStartChar( c ) )
                {
                    int i = 1;
                    while( i < identifier.Length )
                        if( !IsIdentifierChar( identifier[i++] ) ) break;
                    if( i == identifier.Length ) return false;
                }
            }
            return true;
        }
    }

}
