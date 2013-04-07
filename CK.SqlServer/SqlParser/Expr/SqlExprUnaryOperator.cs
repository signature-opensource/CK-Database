using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprUnaryOperator : SqlExpr
    {
        readonly IAbstractExpr[] _components;
        
        public SqlExprUnaryOperator( SqlToken op, SqlExpr rightExpr )
        {
            if( op == null ) throw new ArgumentNullException( "op" );
            if( rightExpr == null ) throw new ArgumentNullException( "rightExpr" );
            _components = CreateArray( op, rightExpr );
        }

        public SqlToken Operator { get { return (SqlToken)_components[0]; } }

        public SqlExpr Expression { get { return (SqlExpr)_components[1]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
