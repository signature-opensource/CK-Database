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
    public class SqlExprIsNull : SqlExpr, ISqlExprEnclosable
    {
        readonly IAbstractExpr[] _components;

        public SqlExprIsNull( SqlExpr left, SqlTokenTerminal isToken, SqlTokenTerminal notToken, SqlTokenIdentifier nullToken )
        {
            _components = notToken != null 
                ? CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, isToken, notToken, nullToken, SqlExprMultiToken<SqlTokenClosePar>.Empty )
                : CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, isToken, nullToken, SqlExprMultiToken<SqlTokenClosePar>.Empty );
        }

        internal SqlExprIsNull( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExprMultiToken<SqlTokenOpenPar> Opener { get { return (SqlExprMultiToken<SqlTokenOpenPar>)_components[0]; } }

        public SqlExpr Left { get { return (SqlExpr)_components[1]; } }

        public SqlTokenTerminal IsToken { get { return (SqlTokenTerminal)_components[2]; } }

        public bool IsNotNull { get { return _components.Length == 6; } }

        public SqlTokenTerminal NotToken { get { return IsNotNull ? (SqlTokenTerminal)_components[3] : null; } }

        public SqlTokenIdentifier NullToken { get { return (SqlTokenIdentifier)_components[IsNotNull ? 4 : 3]; } }

        public SqlExprMultiToken<SqlTokenClosePar> Closer { get { return (SqlExprMultiToken<SqlTokenClosePar>)_components[IsNotNull ? 5 : 4]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        public bool CanEnclose { get { return true; } }

        public ISqlExprEnclosable Enclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            return new SqlExprIsNull( CreateArray( openPar, _components, closePar ) );
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
