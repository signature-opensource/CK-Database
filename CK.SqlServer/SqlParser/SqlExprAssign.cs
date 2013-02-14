using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlAssignExpr : SqlExprBaseBinary
    {
        public SqlAssignExpr( SqlExprIdentifier identifier, SqlTokenTerminal assignToken, SqlExpr right )
            : base( identifier, assignToken, right )
        {
            if( (assignToken.TokenType & SqlTokenType.IsAssignOperator) == 0 ) throw new ArgumentException( "Invalid assign token.", "assignToken" );
        }

        public new SqlExprIdentifier Left { get { return (SqlExprIdentifier)base.Left; } }

        public SqlToken AssignToken { get { return Middle; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
