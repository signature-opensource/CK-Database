using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public abstract class SqlExprBaseBinary : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        protected SqlExprBaseBinary( SqlExpr left, IAbstractExpr middle, SqlExpr right )
        {
            if( left == null ) throw new ArgumentNullException( "left" );
            if( middle == null ) throw new ArgumentNullException( "middle" );
            if( right == null ) throw new ArgumentNullException( "right" );
            _components = CreateArray( left, middle, right );
        }

        protected SqlExprBaseBinary( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExpr Left { get { return (SqlExpr)_components[0]; } }

        protected IAbstractExpr Middle { get { return _components[1]; } }

        public SqlExpr Right { get { return (SqlExpr)_components[2]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

    }

}
