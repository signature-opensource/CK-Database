using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// Base class for literal numbers (<see cref="SqlTokenLiteralBinary"/>, <see cref="SqlTokenLiteralDecimal"/>, <see cref="SqlTokenLiteralFloat"/>, <see cref="SqlTokenLiteralInteger"/>, <see cref="SqlTokenLiteralMoney"/>)
    /// and strings <see cref="SqlTokenLiteralString"/> (either N'unicode' or 'ansi').
    /// </summary>
    public abstract class SqlTokenLiteral : SqlToken
    {
        public SqlTokenLiteral( SqlTokenType t, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( (t & (SqlTokenType.IsString|SqlTokenType.IsNumber)) == 0 ) throw new ArgumentException( "Invalid literal token.", "t" );
        }

        public abstract string LiteralValue { get; }

        public override string ToString()
        {
            return LiteralValue;
        }

        protected override void DoWrite( StringBuilder b )
        {
            b.Append( LiteralValue );
        }
    }

}
