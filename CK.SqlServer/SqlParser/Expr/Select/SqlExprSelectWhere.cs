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
    /// Captures the optional "Where ..." select part.
    /// </summary>
    public class SqlExprSelectWhere : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprSelectWhere( SqlTokenIdentifier whereToken, SqlExpr expression )
        {
            _components = CreateArray( whereToken, expression );
        }

        internal SqlExprSelectWhere( IAbstractExpr[] newContent )
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
