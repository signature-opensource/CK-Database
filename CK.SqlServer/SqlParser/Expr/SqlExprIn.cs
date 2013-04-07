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
    public class SqlExprIn : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprIn( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal inToken, SqlExprGenericBlockList values )
        {
            _components = notToken != null 
                            ? CreateArray( left, notToken, inToken, values ) 
                            : CreateArray( left, inToken, values );
        }

        internal SqlExprIn( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExpr Left { get { return (SqlExpr)_components[0]; } }

        public bool IsNotIn { get { return _components.Length == 4; } }

        public SqlTokenTerminal NotToken { get { return IsNotIn ? (SqlTokenTerminal)_components[1] : null; } }

        public SqlTokenTerminal InToken { get { return (SqlTokenTerminal)_components[IsNotIn ? 2 : 1]; } }

        public SqlExprGenericBlockList Values { get { return (SqlExprGenericBlockList)_components[IsNotIn ? 3 : 2]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
