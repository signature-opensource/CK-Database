using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlLiteralIntegerExpr : SqlLiteralExpr
    {
        public SqlLiteralIntegerExpr( SourceLocation location, int value )
            : base( location, SqlToken.Integer )
        {
            Value = value;
        }

        public int Value { get; private set; }

        public override string LiteralValue { get { return Value.ToString( CultureInfo.InvariantCulture ); } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
