using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public sealed class SqlTokenLiteralDecimal : SqlTokenBaseLiteral
    {
        public SqlTokenLiteralDecimal( SqlTokenType t, string value, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( t != SqlTokenType.Decimal ) throw new ArgumentException( "Invalid token type.", "t" );
            if( value == null ) throw new ArgumentNullException( "value" );
            Value = value;
            int precision, scale;

            int iDot = value.IndexOf( '.' );
            if( iDot >= 0 )
            {
                precision = value.Length - 1;
                if( iDot == 1 && value[0] == '0' ) --precision;
                scale = precision - iDot;
            }
            else
            {
                precision = value.Length;
                scale = 0;
            }
            Precision = (byte)precision;
            Scale = (byte)scale;
        }

        public string Value { get; private set; }

        public byte Precision { get; private set; }

        public byte Scale { get; private set; }

        public override string LiteralValue { get { return Value; } }

    }

}
