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
    /// Captures the optional "Group by ... having ..." select part.
    /// Even if it seems that "having" can exist without "group by" clause, I have not found any use of it: I decided to subordinate the "having" to the "group by".
    /// </summary>
    public class SelectGroupBy : SqlNoExpr
    {
        public SelectGroupBy( SqlTokenIdentifier groupToken, SqlTokenIdentifier byT, SqlExpr groupContent, SqlTokenIdentifier havingT = null, SqlExpr havingExpression = null )
            : this( Build( groupToken, byT, groupContent, havingT, havingExpression ) )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier groupToken, SqlTokenIdentifier byT, SqlExpr groupContent, SqlTokenIdentifier havingT = null, SqlExpr havingExpression = null )
        {
            if( havingT != null )
            {
                if( havingExpression == null ) throw new ArgumentNullException( "havingExpression" );
                return CreateArray( groupToken, byT, groupContent, havingT, havingExpression );
            }
            return CreateArray( groupToken, byT, groupContent );
        }

        internal SelectGroupBy( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExpr GroupExpression { get { return (SqlExpr)Slots[2]; } }

        public SqlExpr HavingExpression { get { return Slots.Length > 3 ? (SqlExpr)Slots[4] : null; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
