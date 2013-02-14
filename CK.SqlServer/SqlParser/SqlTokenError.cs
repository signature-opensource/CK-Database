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
    /// Error tokens are bound to a <see cref="TokenType"/> that is a <see cref="SqlTokenTypeError"/>.
    /// </summary>
    public abstract class SqlTokenError : SqlToken
    {
        public SqlTokenError( SqlTokenTypeError t, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( (SqlTokenType)t, leadingTrivia, trailingTrivia )
        {
            if( t >= 0 || t == SqlTokenTypeError.EndOfInput ) throw new ArgumentException( "Invalid error token type." );
        }

        public new SqlTokenTypeError TokenType { get { return (SqlTokenTypeError)base.TokenType; } }

        protected override void DoWrite( StringBuilder b )
        {
        }

        public override string ToString()
        {
            return TokenType.ToString();
        }
    }

}
