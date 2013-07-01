using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public sealed class SqlTokenLiteralString : SqlTokenBaseLiteral
    {
        public SqlTokenLiteralString( SqlTokenType t, string value, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( (t & SqlTokenType.IsString) == 0 ) throw new ArgumentException( "Invalid token type.", "t" );
            if( value == null ) throw new ArgumentNullException( "value" );
            Value = value;
        }

        public bool IsUnicode { get { return TokenType == SqlTokenType.UnicodeString; } }

        public string Value { get; private set; }

        public override string LiteralValue { get { return String.Format( IsUnicode ? "N'{0}'" : "'{0}'", SqlHelper.SqlEncode( Value ) ); } }

    }

}
