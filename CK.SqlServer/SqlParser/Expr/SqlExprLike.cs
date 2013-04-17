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
    public class SqlExprLike : SqlExpr, ISqlExprEnclosable
    {
        readonly IAbstractExpr[] _components;

        public SqlExprLike( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal likeToken, SqlExpr pattern, SqlTokenIdentifier escapeToken = null, SqlTokenLiteralString escapeChar = null )
        {
            if( notToken == null )
            {
                if( escapeToken == null )
                {
                    _components = CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, likeToken, pattern, SqlExprMultiToken<SqlTokenClosePar>.Empty );
                }
                else
                {
                    if( escapeChar == null ) throw new ArgumentNullException( "escape" );
                    _components = CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, likeToken, pattern, escapeToken, escapeChar, SqlExprMultiToken<SqlTokenClosePar>.Empty );
                }
            }
            else
            {
                if( escapeToken == null )
                {
                    _components = CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, notToken, likeToken, pattern, SqlExprMultiToken<SqlTokenClosePar>.Empty );
                }
                else
                {
                    if( escapeChar == null ) throw new ArgumentNullException( "escape" );
                    _components = CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, notToken, likeToken, pattern, escapeToken, escapeChar, SqlExprMultiToken<SqlTokenClosePar>.Empty );
                }
            }
        }

        internal SqlExprLike( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExprMultiToken<SqlTokenOpenPar> Opener { get { return (SqlExprMultiToken<SqlTokenOpenPar>)_components[0]; } }

        public SqlExpr Left { get { return (SqlExpr)_components[1]; } }

        public bool IsNotLike { get { return _components.Length == 6 || _components.Length == 8; } }

        public SqlTokenTerminal NotToken { get { return IsNotLike ? (SqlTokenTerminal)_components[2] : null; } }

        public SqlTokenTerminal LikeToken { get { return (SqlTokenTerminal)_components[IsNotLike ? 3 : 2]; } }

        public SqlExpr Pattern { get { return (SqlExpr)_components[IsNotLike ? 4 : 3]; } }

        public bool HasEscape { get { return _components.Length > 6; } }

        public SqlTokenIdentifier EscapeToken { get { return HasEscape ? (SqlTokenIdentifier)_components[IsNotLike ? 5 : 4] : null; } }

        public SqlTokenLiteralString EscapeChar { get { return HasEscape ? (SqlTokenLiteralString)_components[IsNotLike ? 6 : 5] : null; } }

        public SqlExprMultiToken<SqlTokenClosePar> Closer { get { return (SqlExprMultiToken<SqlTokenClosePar>)_components[_components.Length - 1]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        public bool CanEnclose { get { return true; } }

        public ISqlExprEnclosable Enclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            return new SqlExprLike( CreateArray( openPar, _components, closePar ) );
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
