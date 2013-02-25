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
            if( IsVariable && name[0] != '@' ) throw new ArgumentException( "Invalid variable name.", "name" );
            _name = name;
        }

        /// <summary>
        /// True if the <see cref="Name"/> is a reserved keyword regardless of whether this <see cref="SqlTokenIdentifier"/> is <see cref="IsQuoted"/> or not:
        /// [int], "int" or int are all keyword names. 
        /// </summary>
        public bool IsKeywordName { get { return (TokenType & ~SqlTokenType.IdentifierQuoteMask) == SqlTokenType.IdentifierReservedKeyword; } }

        /// <summary>
        /// True for keyword names like int or output (but not for [int], NotAKeyword or "output"). 
        /// </summary>
        public bool IsUnquotedKeyword { get { return TokenType == SqlTokenType.IdentifierReservedKeyword; } }

        /// <summary>
        /// True for type names like int or [datetime2] or sql_variant. 
        /// </summary>
        public bool IsTypeName { get { return (int)(TokenType&SqlTokenType.IdentifierMask) > 2; } }

        /// <summary>
        /// True if this <see cref="SqlTokenIdentifier"/> is [quoted] or "quoted".
        /// </summary>
        public bool IsQuoted { get { return (TokenType & SqlTokenType.IdentifierQuoteMask) != 0; } }

        /// <summary>
        /// True if this <see cref="SqlTokenIdentifier"/> is a @Variable.
        /// </summary>
        public bool IsVariable { get { return TokenType == SqlTokenType.IdentifierVariable; } }

        public SqlTokenIdentifier RemoveQuoteIfPossible( bool keepIfReservedKeyword )
        {
            // Already quote free.
            if( (TokenType & SqlTokenType.IdentifierQuoteMask) == 0 ) return this;
            
            // Quotes exist.
            
            // If it is a known (reserved) keyword and it must be preserved, do not do anything.
            if( keepIfReservedKeyword && (TokenType & SqlTokenType.IdentifierMask) != 0 ) return this;

            // Quotes can be removed:
            // - If the identifier is a known (reserved) keyword like [int].
            //      OR 
            // - If the name itself does not require quotes (like [Space with dots...]). 
            if( (TokenType & SqlTokenType.IdentifierMask) != 0 || !SqlTokenizer.IsQuoteRequired( Name ) )
            {
                return new SqlTokenIdentifier( TokenType & ~SqlTokenType.IdentifierQuoteMask, Name, LeadingTrivia, TrailingTrivia );
            }
            return this;
        }

        public string Name { get { return _name; } }

        public bool NameEquals( string name )
        { 
            return String.Compare( _name, name, StringComparison.OrdinalIgnoreCase ) == 0; 
        }

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
