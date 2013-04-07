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
    public class SqlExprSelectGroupBy : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprSelectGroupBy( SqlTokenIdentifier groupToken, SqlTokenIdentifier byToken, SqlExprGenericBlockList groupContent, SqlTokenIdentifier havingToken = null, SqlExpr havingExpression = null )
        {
            if( havingToken != null )
            {
                if( havingExpression == null ) throw new ArgumentNullException( "havingExpression" );
                _components = CreateArray( groupToken, byToken, groupContent, havingToken, havingExpression );
            }
            else _components = CreateArray( groupToken, byToken, groupContent );
        }

        internal SqlExprSelectGroupBy( IAbstractExpr[] newContent )
        {
            _components = newContent;
        }

        public override IEnumerable<IAbstractExpr> Components
        {
            get { return _components; }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
