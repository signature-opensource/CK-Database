#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Tokenizer\SqlTrivia.cs) is part of CK-Database. 
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
    public struct SqlTrivia
    {
        /// <summary>
        /// A single space.
        /// </summary>
        public static readonly IReadOnlyList<SqlTrivia> OneSpace = new CKReadOnlyListMono<SqlTrivia>( new SqlTrivia( SqlTokenType.None, " " ) );

        public SqlTrivia( SqlTokenType tokenType, string text )
        {
            if( tokenType != SqlTokenType.None && tokenType != SqlTokenType.LineComment && tokenType != SqlTokenType.StarComment )
            {
                throw new ArgumentException( "Must be none, star or line comment.", "tokenType" );
            }
            if( text == null ) throw new ArgumentNullException( "text" );
            
            TokenType = tokenType;
            Text = text;
        }

        /// <summary>
        /// Gets a token type that can be <see cref="SqlTokenType.None"/> for white space
        /// or <see cref="SqlTokenType.LineComment"/> or <see cref="SqlTokenType.StarComment"/>. 
        /// </summary>
        readonly public SqlTokenType TokenType;
        
        /// <summary>
        /// Gets the text of this trivia. When it is a <see cref="SqlTokenType.LineComment"/> or <see cref="SqlTokenType.StarComment"/>,
        /// the -- or /* */ characters do not appear.
        /// </summary>
        readonly public string Text;

        public override int GetHashCode()
        {
            return Util.Hash.Combine( (long)TokenType, Text.GetHashCode()  ).GetHashCode();
        }

        public override bool Equals( object obj )
        {
            if( obj is SqlTrivia )
            {
                SqlTrivia t = (SqlTrivia)obj;
                return t.TokenType == TokenType && t.Text == Text;
            }
            return false;
        }

        public override string ToString()
        {
            switch( TokenType )
            {
                case SqlTokenType.LineComment: return "--" + Text + Environment.NewLine;
                case SqlTokenType.StarComment: return "/*" + Text + "*/";
            }
            return Text;
        }

        public void Write( StringBuilder b )
        {
            switch( TokenType )
            {
                case SqlTokenType.LineComment: b.Append( "--" ).Append( Text ).Append( Environment.NewLine ); break;
                case SqlTokenType.StarComment: b.Append( "/*" ).Append( Text ).Append( "*/" ); break;
                default: b.Append( Text ); break;
            }
        }
    }

}
