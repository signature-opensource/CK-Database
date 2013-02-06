using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlAssignExpr : SqlBinaryExpr
    {
        public SqlAssignExpr( SourceLocation location, SqlIdentifierExpr left, SqlToken t, SqlExpr right )
            : base( location, left, right )
        {
            if( t < 0 || (t & SqlToken.IsAssignOperator) == 0 ) throw new ArgumentException( "Invalid assign token.", "t" );
            AssignToken = t;
        }

        public new SqlIdentifierExpr Left { get { return (SqlIdentifierExpr)base.Left; } }

        public SqlToken AssignToken { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            return String.Format( "{0} {1} {2}", Left, SqlTokeniser.Explain( AssignToken ), Right );
        }
    }
}
