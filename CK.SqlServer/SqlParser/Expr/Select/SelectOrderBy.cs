using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// Captures the optional "Order by ..." select part.
    /// </summary>
    public class SelectOrderBy : SqlNoExpr
    {
        public SelectOrderBy( SqlTokenIdentifier orderToken, SqlTokenIdentifier byToken, SqlExpr content )
            : this( CreateArray( orderToken, byToken, content ) )
        {
        }

        internal SelectOrderBy( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExpr Expression { get { return (SqlExpr)Slots[2]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
