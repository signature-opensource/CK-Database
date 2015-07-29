#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Tokenizer\SqlToken.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using CK.Core;
using System.Diagnostics;
using System.Globalization;

namespace CK.SqlServer.Parser
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
            if( tokenType > 0 && ((tokenType & SqlTokenType.TokenDiscriminatorMask) == 0 || (tokenType&SqlTokenType.IsComment) !=0) ) throw new ArgumentException( "Invalid token type." );
            
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
        /// Writes the token without its leading nor traling trivias.
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

        SqlToken ISqlItem.LastOrEmptyT { get { return this; } }

        SqlToken ISqlItem.FirstOrEmptyT { get { return this; } }

        /// <summary>
        /// Empty parenthesis opener.
        /// </summary>
        static public readonly SqlExprMultiToken<SqlTokenOpenPar> EmptyOpenPar = SqlExprMultiToken<SqlTokenOpenPar>.Empty;

        /// <summary>
        /// Empty parenthesis closer.
        /// </summary>
        static public readonly SqlExprMultiToken<SqlTokenClosePar> EmptyClosePar = SqlExprMultiToken<SqlTokenClosePar>.Empty;

        /// <summary>
        /// True if the <see cref="SqlToken"/> is the terminator ; token or a <see cref="SqlTokenType.IdentifierReservedStatement"/>.
        /// </summary>
        /// <param name="t">Token to test.</param>
        /// <returns>Whether the token is the statement terminator or the possible start of a new statement.</returns>
        static public bool IsTerminatorOrPossibleStartStatement( SqlToken t )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
            return t.TokenType == SqlTokenType.SemiColon
                    || (t.TokenType & SqlTokenType.IdentifierTypeMask) == SqlTokenType.IdentifierStandardStatement
                    || (t.TokenType & SqlTokenType.IdentifierTypeMask) == SqlTokenType.IdentifierReservedStatement;
        }

        internal static bool IsIdentifierStartChar( int c )
        {
            return c == '@' || c == '#' || c == '$' || c == '_' || Char.IsLetter( (char)c );
        }

        internal static bool IsIdentifierChar( int c )
        {
            return IsIdentifierStartChar( c ) || Char.IsDigit( (char)c );
        }

        /// <summary>
        /// Tests whether an identifier must be quoted (it is empty, starts with @, or $ or contains a character that is not valid).
        /// </summary>
        /// <param name="identifier">Identifier to test.</param>
        /// <returns>True if the identifier can be used without surrounding quotes.</returns>
        static public bool IsQuoteRequired( string identifier )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( identifier.Length > 0 )
            {
                char c = identifier[0];
                if( c != '@' && c != '$' && IsIdentifierStartChar( c ) )
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
