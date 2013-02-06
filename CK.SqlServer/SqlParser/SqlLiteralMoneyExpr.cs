using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlLiteralMoneyExpr : SqlLiteralExpr
    {
        public SqlLiteralMoneyExpr( SourceLocation location, string value )
            : base( location, SqlToken.Money )
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
