using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlNullExpr : SqlLiteralExpr
    {
        public SqlNullExpr( SourceLocation location )
            : base( location, SqlToken.Identifier )
        {
        }

        public override string LiteralValue { get { return "null"; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
