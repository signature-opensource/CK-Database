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
    /// Captures the optional "From ..." select part.
    /// </summary>
    public class SqlExprSelectFrom : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprSelectFrom( SqlTokenIdentifier fromToken, SqlExprGenericBlockList fromContent )
        {
            _components = CreateArray( fromToken, fromContent );
        }

        internal SqlExprSelectFrom( IAbstractExpr[] newContent )
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
