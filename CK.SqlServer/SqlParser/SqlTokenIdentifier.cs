using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlTokenIdentifier : SqlToken
    {
        readonly string _name;

        public SqlTokenIdentifier( SqlTokenType t, string name, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( (t&SqlTokenType.IsIdentifier) == 0 ) throw new ArgumentException( "Invalid token type.", "t" );
            if( String.IsNullOrWhiteSpace( name ) ) throw new ArgumentNullException( "name" );
            if( IsVariable && Name[0] != '@' ) throw new ArgumentException( "Invalid variable name.", "name" );
            _name = name;
        }

        /// <summary>
        /// True if the <see cref="Name"/> is a reserved keyword regardless of whether this <see cref="SqlTokenIdentifier"/> is <see cref="IsQuoted"/> or not:
        /// [int], "int" or int are all keyword names. 
        /// </summary>
        public bool IsKeywordName { get { return (TokenType & ~SqlTokenType.IdentifierQuoteMask) == SqlTokenType.IdentifierTypeReservedKeyword; } }

        /// <summary>
        /// True for keyword names like int or output (but not for [int], NotAKeyword or "output"). 
        /// </summary>
        public bool IsUnquotedKeyword { get { return TokenType == SqlTokenType.IdentifierTypeReservedKeyword; } }

        /// <summary>
        /// True if this <see cref="SqlTokenIdentifier"/> is [quoted] or "quoted".
        /// </summary>
        public bool IsQuoted { get { return (TokenType & SqlTokenType.IdentifierQuoteMask) != 0; } }

        /// <summary>
        /// True if this <see cref="SqlTokenIdentifier"/> is a @Variable.
        /// </summary>
        public bool IsVariable { get { return TokenType == SqlTokenType.IdentifierTypeVariable; } }

        public SqlTokenIdentifier RemoveQuoteIfPossible()
        {
            // Already quote free.
            if( (TokenType & SqlTokenType.IdentifierQuoteMask) == 0 ) return this;
            // Quotes exist but they are required (if the identifier is a known identifier - like [int], quotes can be removed).
            if( (TokenType & SqlTokenType.IdentifierTypeMask) == 0 && !SqlTokeniser.IsQuoteRequired( Name ) ) return this;
            // Quotes can be removed:
            return new SqlTokenIdentifier( TokenType & ~SqlTokenType.IdentifierQuoteMask, Name, LeadingTrivia, TrailingTrivia );
        }

        public string Name { get { return _name; } }

        protected override void DoWrite( StringBuilder b )
        {
            switch( TokenType&SqlTokenType.IdentifierQuoteMask )
            {
                case SqlTokenType.IsIdentifierQuoted: b.Append( "\"" ).Append( Name.Replace( "\"", "\"\"" ) ).Append( "\"" ); break;
                case SqlTokenType.IsIdentifierQuotedBracket: b.Append( "[" ).Append( Name.Replace( "]", "]]" ) ).Append( "]" ); break;
                default: b.Append( Name ); break;
            }
        }

    }


}
