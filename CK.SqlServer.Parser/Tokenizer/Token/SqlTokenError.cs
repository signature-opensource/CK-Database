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
    /// Error tokens are bound to a <see cref="TokenType"/> that is a <see cref="SqlTokenTypeError"/>.
    /// </summary>
    public class SqlTokenError : SqlToken
    {
        public static readonly SqlTokenError EndOfInput = new SqlTokenError( SqlTokenTypeError.EndOfInput, null, null );

        public SqlTokenError( SqlTokenTypeError t, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( (SqlTokenType)t, leadingTrivia, trailingTrivia )
        {
            if( t >= 0 ) throw new ArgumentException( "Invalid error token type." );
            ErrorMessage = t.ToString();
        }

        public SqlTokenError( string message )
            : base( SqlTokenType.ErrorMask, null, null )
        {
            if( String.IsNullOrWhiteSpace( message ) ) throw new ArgumentNullException( "message" );
            ErrorMessage = message;
        }

        public string ErrorMessage { get; private set; }

        public new SqlTokenTypeError TokenType { get { return (SqlTokenTypeError)base.TokenType; } }
        
        public bool IsEndOfInput { get { return base.TokenType == SqlTokenType.EndOfInput; } }

        protected override void DoWrite( StringBuilder b )
        {
            b.AppendFormat( "[Error: {0}]", ErrorMessage ); 
        }
    }

}
