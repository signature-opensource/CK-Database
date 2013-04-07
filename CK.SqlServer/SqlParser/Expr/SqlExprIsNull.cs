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
    /// 
    /// </summary>
    public class SqlExprIsNull : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprIsNull( SqlExpr left, SqlTokenTerminal isToken, SqlTokenTerminal notToken, SqlTokenIdentifier nullToken )
        {
            _components = notToken != null ? CreateArray( left, isToken, notToken, nullToken ) : CreateArray( left, isToken, nullToken );
        }

        public SqlExpr Left { get { return (SqlExpr)_components[0]; } }

        public SqlTokenTerminal IsToken { get { return (SqlTokenTerminal)_components[1]; } }

        public bool IsNotNull { get { return _components.Length == 4; } }

        public SqlTokenTerminal NotToken { get { return IsNotNull ? (SqlTokenTerminal)_components[2] : null; } }

        public SqlTokenIdentifier NullToken { get { return (SqlTokenIdentifier)_components[IsNotNull ? 3 : 2]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
