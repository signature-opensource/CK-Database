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
    /// Captures the optional "INTO table".
    /// </summary>
    public class SqlExprSelectInto : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprSelectInto( SqlTokenIdentifier intoToken, SqlExprMultiIdentifier tableName )
        {
            _components = CreateArray( intoToken, tableName );
        }

        internal SqlExprSelectInto( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
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
