using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlTokenLiteralMoney : SqlTokenLiteral
    {
        public SqlTokenLiteralMoney( SqlTokenType t, string value, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( t != SqlTokenType.Money ) throw new ArgumentException( "Invalid token type.", "t" );
            Value = value;
        }

        public string Value { get; private set; }

        public override string LiteralValue { get { return Value; } }
    }

}
