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
    public class SqlExprBetween : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprBetween( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal betweenToken, SqlExpr start, SqlTokenTerminal andToken, SqlExpr stop )
        {
            _components = notToken != null 
                            ? CreateArray( left, notToken, betweenToken, start, andToken, stop ) 
                            : CreateArray( left, betweenToken, start, andToken, stop );
        }

        internal SqlExprBetween( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExpr Left { get { return (SqlExpr)_components[0]; } }

        public bool IsNotBetween { get { return _components.Length == 6; } }

        public SqlTokenTerminal NotToken { get { return IsNotBetween ? (SqlTokenTerminal)_components[1] : null; } }

        public SqlTokenTerminal BetweenToken { get { return (SqlTokenTerminal)_components[IsNotBetween ? 2 : 1]; } }

        public SqlExpr Start { get { return (SqlExpr)_components[IsNotBetween ? 3 : 2]; } }

        public SqlTokenTerminal AndToken { get { return (SqlTokenTerminal)_components[IsNotBetween ? 4 : 3]; } }

        public SqlExpr Stop { get { return (SqlExpr)_components[IsNotBetween ? 5 : 4]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
