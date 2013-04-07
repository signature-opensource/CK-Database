using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprKoCall : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprKoCall( SqlExpr funName, ISqlExprList<SqlExpr> parameters )
        {
            if( funName == null ) throw new ArgumentNullException( "targetName" );
            if( parameters == null ) throw new ArgumentNullException( "parameters" );
            _components = CreateArray( funName, parameters );
        }

        internal SqlExprKoCall( SqlExprKoCall origin, IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExpr FunName { get { return (SqlExpr)_components[0]; } }

        public ISqlExprList<SqlExpr> Parameters { get { return (ISqlExprList<SqlExpr>)_components[1]; } }

        public override sealed IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
