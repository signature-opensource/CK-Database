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
    public class SqlExprLike : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprLike( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal likeToken, SqlExpr pattern, SqlTokenIdentifier escapeToken = null, SqlTokenLiteralString escapeChar = null )
        {
            if( notToken == null )
            {
                if( escapeToken == null )
                {
                    _components = CreateArray( left, likeToken, pattern );
                }
                else
                {
                    if( escapeChar == null ) throw new ArgumentNullException( "escape" );
                    _components = CreateArray( left, likeToken, pattern, escapeToken, escapeChar );
                }
            }
            else
            {
                if( escapeToken == null )
                {
                    _components = CreateArray( left, notToken, likeToken, pattern );
                }
                else
                {
                    if( escapeChar == null ) throw new ArgumentNullException( "escape" );
                    _components = CreateArray( left, notToken, likeToken, pattern, escapeToken, escapeChar );
                }
            }
        }

        internal SqlExprLike( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExpr Left { get { return (SqlExpr)_components[0]; } }

        public bool IsNotLike { get { return _components.Length == 4 || _components.Length == 6; } }

        public SqlTokenTerminal NotToken { get { return IsNotLike ? (SqlTokenTerminal)_components[1] : null; } }

        public SqlTokenTerminal LikeToken { get { return (SqlTokenTerminal)_components[IsNotLike ? 2 : 1]; } }

        public SqlExpr Pattern { get { return (SqlExpr)_components[IsNotLike ? 3 : 2]; } }

        public bool HasEscape { get { return _components.Length > 4; } }

        public SqlTokenIdentifier EscapeToken { get { return HasEscape ? (SqlTokenIdentifier)_components[IsNotLike ? 4 : 3] : null; } }

        public SqlTokenLiteralString EscapeChar { get { return HasEscape ? (SqlTokenLiteralString)_components[IsNotLike ? 5 : 4] : null; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
