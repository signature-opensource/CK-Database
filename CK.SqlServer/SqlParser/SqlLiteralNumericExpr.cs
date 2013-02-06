using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlLiteralNumericExpr : SqlLiteralExpr
    {
        public SqlLiteralNumericExpr( SourceLocation location, string value )
            : base( location, SqlToken.Decimal )
        {
            Value = value;
        }

        public string Value { get; private set; }

        public override string LiteralValue { get { return Value; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
