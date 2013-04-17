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
    public class SqlExprIn : SqlExpr, ISqlExprEnclosable
    {
        readonly IAbstractExpr[] _components;

        public SqlExprIn( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal inToken, SqlExprList values )
        {
            _components = notToken != null
                            ? CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, notToken, inToken, values, SqlExprMultiToken<SqlTokenClosePar>.Empty )
                            : CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, inToken, values, SqlExprMultiToken<SqlTokenClosePar>.Empty );
        }

        internal SqlExprIn( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExprMultiToken<SqlTokenOpenPar> Opener { get { return (SqlExprMultiToken<SqlTokenOpenPar>)_components[0]; } }

        public SqlExpr Left { get { return (SqlExpr)_components[1]; } }

        public bool IsNotIn { get { return _components.Length == 6; } }

        public SqlTokenTerminal NotToken { get { return IsNotIn ? (SqlTokenTerminal)_components[2] : null; } }

        public SqlTokenTerminal InToken { get { return (SqlTokenTerminal)_components[IsNotIn ? 3 : 2]; } }

        public SqlExprList Values { get { return (SqlExprList)_components[IsNotIn ? 4 : 3]; } }

        public SqlExprMultiToken<SqlTokenClosePar> Closer { get { return (SqlExprMultiToken<SqlTokenClosePar>)_components[IsNotIn ? 5 : 4]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        public bool CanEnclose { get { return true; } }

        public ISqlExprEnclosable Enclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            return new SqlExprIn( CreateArray( openPar, _components, closePar ) );
        }

        public IEnumerable<IAbstractExpr> ComponentsWithoutParenthesis
        {
            get { return _components.Skip( 1 ).Take( _components.Length - 2 ); }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
