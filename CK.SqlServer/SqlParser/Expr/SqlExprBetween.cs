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
    public class SqlExprBetween : SqlExpr, ISqlExprEnclosable
    {
        readonly IAbstractExpr[] _components;

        public SqlExprBetween( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal betweenToken, SqlExpr start, SqlTokenTerminal andToken, SqlExpr stop )
        {
            _components = notToken != null
                            ? CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, notToken, betweenToken, start, andToken, stop, SqlExprMultiToken<SqlTokenClosePar>.Empty )
                            : CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, betweenToken, start, andToken, stop, SqlExprMultiToken<SqlTokenClosePar>.Empty );
        }

        internal SqlExprBetween( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExprMultiToken<SqlTokenOpenPar> Opener { get { return (SqlExprMultiToken<SqlTokenOpenPar>)_components[0]; } }

        public SqlExpr Left { get { return (SqlExpr)_components[1]; } }

        public bool IsNotBetween { get { return _components.Length == 8; } }

        public SqlTokenTerminal NotToken { get { return IsNotBetween ? (SqlTokenTerminal)_components[2] : null; } }

        public SqlTokenTerminal BetweenToken { get { return (SqlTokenTerminal)_components[IsNotBetween ? 3 : 2]; } }

        public SqlExpr Start { get { return (SqlExpr)_components[IsNotBetween ? 4 : 3]; } }

        public SqlTokenTerminal AndToken { get { return (SqlTokenTerminal)_components[IsNotBetween ? 5 : 4]; } }

        public SqlExpr Stop { get { return (SqlExpr)_components[IsNotBetween ? 6 : 5]; } }

        public SqlExprMultiToken<SqlTokenClosePar> Closer { get { return (SqlExprMultiToken<SqlTokenClosePar>)_components[_components.Length-1]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        public bool CanEnclose { get { return true; } }

        public ISqlExprEnclosable Enclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            return new SqlExprBetween( CreateArray( openPar, _components, closePar ) );
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
