using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Captures the optional "Where ..." select part.
    /// </summary>
    public class SelectWhere : SqlNoExpr
    {
        public SelectWhere( SqlTokenIdentifier whereToken, SqlExpr expression )
            : this( CreateArray( whereToken, expression ) )
        {
        }

        internal SelectWhere( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlTokenIdentifier WhereToken { get { return (SqlTokenIdentifier)Slots[0]; } }
        
        public SqlExpr Expression { get { return (SqlExpr)Slots[1]; } }


        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
