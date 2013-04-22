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
    /// Captures the optional "Group by ... having ..." select part.
    /// Even if it seems that "having" can exist without "group by" clause, I have not found any use of it: I decided to subordinate the "having" to the "group by".
    /// </summary>
    public class SelectGroupBy : SqlNoExpr
    {
        public SelectGroupBy( SqlTokenIdentifier groupToken, SqlTokenIdentifier byToken, SqlExpr groupContent, SqlTokenIdentifier havingToken = null, SqlExpr havingExpression = null )
            : this( Build( groupToken, byToken, groupContent, havingToken, havingExpression ) )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier groupToken, SqlTokenIdentifier byToken, SqlExpr groupContent, SqlTokenIdentifier havingToken = null, SqlExpr havingExpression = null )
        {
            if( havingToken != null )
            {
                if( havingExpression == null ) throw new ArgumentNullException( "havingExpression" );
                return CreateArray( groupToken, byToken, groupContent, havingToken, havingExpression );
            }
            return CreateArray( groupToken, byToken, groupContent );
        }

        internal SelectGroupBy( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExpr GroupExpression { get { return (SqlExpr)Slots[2]; } }

        public SqlExpr HavingExpression { get { return Slots.Length > 3 ? (SqlExpr)Slots[4] : null; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
