using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Base class for literal numbers (<see cref="SqlTokenLiteralBinary"/>, <see cref="SqlTokenLiteralDecimal"/>, <see cref="SqlTokenLiteralFloat"/>, 
    /// <see cref="SqlTokenLiteralInteger"/>, <see cref="SqlTokenLiteralMoney"/>)
    /// and strings <see cref="SqlTokenLiteralString"/> (either N'unicode' or 'ansi').
    /// </summary>
    public abstract class SqlTokenBaseLiteral : SqlToken
    {
        public SqlTokenBaseLiteral( SqlTokenType t, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( (t & (SqlTokenType.IsString|SqlTokenType.IsNumber)) == 0 ) throw new ArgumentException( "Invalid literal token.", "t" );
        }

        /// <summary>
        /// Gets the literal form of this token.
        /// This may be a slightly modified string as the raw, original, text.
        /// </summary>
        public abstract string LiteralValue { get; }

        /// <summary>
        /// By default, the string is the <see cref="LiteralValue"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return LiteralValue;
        }

        /// <summary>
        /// Simply appends the <see cref="LiteralValue"/>.
        /// </summary>
        /// <param name="b">The builder to use.</param>
        protected override void DoWrite( StringBuilder b )
        {
            b.Append( LiteralValue );
        }
    }

}
