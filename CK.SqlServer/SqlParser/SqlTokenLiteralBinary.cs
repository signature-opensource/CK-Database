using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlTokenLiteralBinary : SqlTokenLiteral
    {
        public SqlTokenLiteralBinary( SqlTokenType t, string value, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( t != SqlTokenType.Binary ) throw new ArgumentException( "Invalid token type.", "t" );
            if( value == null ) throw new ArgumentNullException( "value" );
            Value = value;
        }

        public string Value { get; private set; }

        public override string LiteralValue { get { return Value; } }

    }

}
