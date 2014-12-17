#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprLike.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
  
    /// <summary>
    /// 
    /// </summary>
    public class SqlExprLike : SqlExpr
    {
        public SqlExprLike( SqlExpr left, SqlTokenIdentifier notToken, SqlTokenIdentifier likeToken, SqlExpr pattern, SqlTokenIdentifier escapeToken = null, SqlTokenLiteralString escapeChar = null )
            : this( Build( left, notToken, likeToken, pattern, escapeToken, escapeChar ) )
        {
        }

        static ISqlItem[] Build( SqlExpr left, SqlTokenIdentifier notToken, SqlTokenIdentifier likeToken, SqlExpr pattern, SqlTokenIdentifier escapeToken = null, SqlTokenLiteralString escapeChar = null )
        {
            if( notToken == null )
            {
                if( escapeToken == null )
                {
                    return CreateArray( SqlToken.EmptyOpenPar, left, likeToken, pattern, SqlToken.EmptyClosePar );
                }
                else
                {
                    if( escapeChar == null ) throw new ArgumentNullException( "escape" );
                    return CreateArray( SqlToken.EmptyOpenPar, left, likeToken, pattern, escapeToken, escapeChar, SqlToken.EmptyClosePar );
                }
            }
            else
            {
                if( escapeToken == null )
                {
                    return CreateArray( SqlToken.EmptyOpenPar, left, notToken, likeToken, pattern, SqlToken.EmptyClosePar );
                }
                else
                {
                    if( escapeChar == null ) throw new ArgumentNullException( "escape" );
                    return CreateArray( SqlToken.EmptyOpenPar, left, notToken, likeToken, pattern, escapeToken, escapeChar, SqlToken.EmptyClosePar );
                }
            }
        }

        internal SqlExprLike( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlExpr Left { get { return (SqlExpr)Slots[1]; } }

        public bool IsNotLike { get { return Slots.Length == 6 || Slots.Length == 8; } }

        public SqlTokenIdentifier NotT { get { return IsNotLike ? (SqlTokenIdentifier)Slots[2] : null; } }

        public SqlTokenIdentifier LikeT { get { return (SqlTokenIdentifier)Slots[IsNotLike ? 3 : 2]; } }

        public SqlExpr Pattern { get { return (SqlExpr)Slots[IsNotLike ? 4 : 3]; } }

        public bool HasEscape { get { return Slots.Length > 6; } }

        public SqlTokenIdentifier EscapeT { get { return HasEscape ? (SqlTokenIdentifier)Slots[IsNotLike ? 5 : 4] : null; } }

        public SqlTokenLiteralString EscapeChar { get { return HasEscape ? (SqlTokenLiteralString)Slots[IsNotLike ? 6 : 5] : null; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
