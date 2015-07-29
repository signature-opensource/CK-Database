#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprCase.cs) is part of CK-Database. 
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
    public class SqlExprCursorSql92 : SqlExpr, ISqlExprCursor
    {
        public SqlExprCursorSql92(
            SqlTokenIdentifier insensitiveOrScrollToken,
            SqlTokenIdentifier scrollOrInsensitiveToken,
            SqlTokenIdentifier cursorToken,
            SqlTokenIdentifier forToken,
            ISelectSpecification select,
            SqlTokenIdentifier forOptionsToken,
            SqlTokenIdentifier readToken,
            SqlTokenIdentifier onlyToken,
            SqlTokenIdentifier updateToken,
            SqlTokenIdentifier ofToken,
            SqlNoExprIdentifierList updateColumns )
            : this( Build( insensitiveOrScrollToken, scrollOrInsensitiveToken, cursorToken, forToken, select, forOptionsToken, readToken, onlyToken, updateToken, ofToken, updateColumns ) )
        {
        }

        static ISqlItem[] Build(
            SqlTokenIdentifier insensitiveOrScrollToken,
            SqlTokenIdentifier scrollOrInsensitiveToken,
            SqlTokenIdentifier cursorToken, 
            SqlTokenIdentifier forToken,
            ISelectSpecification select, 
            SqlTokenIdentifier forOptionsToken, 
            SqlTokenIdentifier readToken, 
            SqlTokenIdentifier onlyToken, 
            SqlTokenIdentifier updateToken, 
            SqlTokenIdentifier ofToken,
            SqlNoExprIdentifierList updateColumns )
        {
            if( insensitiveOrScrollToken != null || scrollOrInsensitiveToken != null ) throw new NotImplementedException( "Sql92 'insensitive' and 'scroll' are not yet implemented." );

            if( cursorToken == null ) throw new ArgumentNullException( "cursorToken" );
            if( forToken == null ) throw new ArgumentNullException( "forToken" );
            if( select == null ) throw new ArgumentNullException( "select" );
            if( forOptionsToken != null )
            {
                if( (readToken == null) == (updateToken == null)) throw new ArgumentException( "Either 'read only' or 'update' must be specified but not both.", "forToken" );
                if( (readToken == null) != (onlyToken == null) ) throw new ArgumentException( "Required 'read only' ('read' or 'only' alone is invalid).", "readToken" );
                if( (ofToken == null) != (updateColumns == null) ) throw new ArgumentException( "Required at least one column name after 'of' or no of and no columns.", "ofToken" );

                if( readToken != null )
                {
                    Debug.Assert( updateToken == null );
                    return CreateArray( SqlToken.EmptyOpenPar, cursorToken, select, forOptionsToken, readToken, onlyToken, SqlToken.EmptyOpenPar );
                }
                else
                {
                    Debug.Assert( updateToken != null );
                    if( ofToken != null )
                    {
                        return CreateArray( SqlToken.EmptyOpenPar, cursorToken, select, forOptionsToken, updateToken, ofToken, updateColumns, SqlToken.EmptyClosePar );
                    }
                    else
                    {
                        return CreateArray( SqlToken.EmptyOpenPar, cursorToken, select, forOptionsToken, updateToken, SqlToken.EmptyClosePar );
                    }
                }
            }
            else
            {
                return CreateArray( SqlToken.EmptyOpenPar, cursorToken, select, SqlToken.EmptyClosePar );
            }
        }

        internal SqlExprCursorSql92( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public bool IsSql92Syntax { get { return true; } }

        /// <summary>
        /// Gets the first case token.
        /// </summary>
        public SqlTokenIdentifier CursorT { get { return (SqlTokenIdentifier)Slots[1]; } }

        /// <summary>
        /// Gets the select specification for this cursor.
        /// </summary>
        public ISelectSpecification Select 
        {
            get { return Slots[2] as SelectSpecification ?? Slots[3] as ISelectSpecification; } 
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
