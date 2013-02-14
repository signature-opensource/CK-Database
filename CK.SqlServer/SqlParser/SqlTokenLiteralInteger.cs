using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlTokenLiteralInteger : SqlTokenLiteral
    {
        public SqlTokenLiteralInteger( SqlTokenType t, int value, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( t != SqlTokenType.Integer ) throw new ArgumentException( "Invalid token type.", "t" );
            Value = value;
        }

        public int Value { get; private set; }

        public override string LiteralValue { get { return Value.ToString( CultureInfo.InvariantCulture ); } }

    }

}
