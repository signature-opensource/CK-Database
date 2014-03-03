using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Captures a select column definition. 
    /// </summary>
    public class SqlNoExprOverClause : SqlNoExpr
    {
        public SqlNoExprOverClause( SqlTokenIdentifier overToken, SqlExpr overExpression )
            : this( CreateArray( overToken, overExpression ) )
        {
        }

        internal SqlNoExprOverClause( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlTokenIdentifier OverToken { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlExpr OverExpression { get { return (SqlExpr)Slots[1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
