using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlLiteralFloatExpr : SqlLiteralExpr
    {
        public SqlLiteralFloatExpr( SourceLocation location, double value )
            : base( location, SqlToken.Float )
        {
            Value = value;
        }

        public double Value { get; private set; }

        public override string LiteralValue { get { return Value.ToString( CultureInfo.InvariantCulture ); } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
