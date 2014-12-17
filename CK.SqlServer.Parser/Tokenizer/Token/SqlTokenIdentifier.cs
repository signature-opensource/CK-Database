#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Tokenizer\Token\SqlTokenIdentifier.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Token for identifiers. An identifier can be <see cref="IsQuoted"/>, be <see cref="IsVariable"/>, be <see cref="IsKeywordName"/>.
    /// </summary>
    public sealed class SqlTokenIdentifier : SqlToken
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
        /// True for star (*) identifier. 
        /// </summary>
        public bool IsStar { get { return TokenType == SqlTokenType.IdentifierStar; } }

        /// <summary>
        /// True for type names like int or sql_variant. 
        /// </summary>
        public bool IsDbType { get { return (TokenType&SqlTokenType.IdentifierTypeMask) == SqlTokenType.IdentifierDbType; } }

        /// <summary>
        /// True if this <see cref="SqlTokenIdentifier"/> is [quoted] or "quoted".
        /// </summary>
        public bool IsQuoted { get { return TokenType == SqlTokenType.IdentifierQuoted || TokenType == SqlTokenType.IdentifierQuotedBracket; } }

        /// <summary>
        /// True if this <see cref="SqlTokenIdentifier"/> is a @Variable or a @@SystemFunction.
        /// </summary>
        public bool IsVariable { get { return TokenType == SqlTokenType.IdentifierVariable; } }

        /// <summary>
        /// True if this <see cref="SqlTokenIdentifier"/> is a reserved keyword that starts a statement (select, create, declare, etc.)
        /// or a standard identifer that also can start a statement (throw, get, move, etc.).
        /// </summary>
        public bool IsStartStatement 
        { 
            get 
            {
                Debug.Assert( SqlTokenType.IdentifierStandardStatement == SqlTokenType.IsIdentifier
                                && SqlTokenType.IdentifierReservedStatement == (SqlTokenType.IsIdentifier + (1 << 11)), "Statement identifiers must be the first ones." );
                return (TokenType & SqlTokenType.IdentifierTypeMask) <= SqlTokenType.IdentifierReservedStatement; 
            } 
        }

        public SqlTokenIdentifier RemoveQuoteIfPossible( bool keepIfReservedKeyword )
        {
            // Already quote free.
            if( !IsQuoted ) return this;
            
            // Quotes exist.
            
            // Are quotes required? If yes, don't do it.
            if( SqlToken.IsQuoteRequired( Name ) ) return this;

            // If it is a known (reserved) keyword and it must be preserved, do not do anything.
            SqlTokenType typeWithoutQuote;
            bool isReservedKeyWord = SqlKeyword.IsReservedKeyword( Name, out typeWithoutQuote );
            if( keepIfReservedKeyword && isReservedKeyWord ) return this;
            if( typeWithoutQuote == SqlTokenType.None ) typeWithoutQuote = SqlTokenType.IdentifierStandard;

            return new SqlTokenIdentifier( typeWithoutQuote, Name, LeadingTrivia, TrailingTrivia );
        }

        public string Name { get { return _name; } }

        public bool NameEquals( string name )
        { 
            return String.Compare( _name, name, StringComparison.OrdinalIgnoreCase ) == 0; 
        }

        protected override void DoWrite( StringBuilder b )
        {
            switch( TokenType )
            {
                case SqlTokenType.IdentifierQuoted: b.Append( "\"" ).Append( Name.Replace( "\"", "\"\"" ) ).Append( "\"" ); break;
                case SqlTokenType.IdentifierQuotedBracket: b.Append( "[" ).Append( Name.Replace( "]", "]]" ) ).Append( "]" ); break;
                default: b.Append( Name ); break;
            }
        }

    }


}
