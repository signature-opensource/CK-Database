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
    public class SqlExprCursor : SqlExpr, ISqlExprCursor
    {

        public SqlExprCursor( 
            SqlTokenIdentifier cursorToken, 
            SqlExprUnmodeledItems options, 
            SqlTokenIdentifier forToken,
            ISelectSpecification select, 
            SqlTokenIdentifier forOptionsToken, 
            SqlTokenIdentifier updateToken, 
            SqlTokenIdentifier ofToken, 
            SqlNoExprIdentifierList updateColumns )
            : this( Build( cursorToken, options, forToken, select, forOptionsToken, updateToken, ofToken, updateColumns ) )
        {
        }

        static ISqlItem[] Build(
            SqlTokenIdentifier cursorToken,
            SqlExprUnmodeledItems options,
            SqlTokenIdentifier forToken,
            ISelectSpecification select,
            SqlTokenIdentifier forOptionsToken,
            SqlTokenIdentifier updateToken,
            SqlTokenIdentifier ofToken,
            SqlNoExprIdentifierList updateColumns )
        {
            if( cursorToken == null ) throw new ArgumentNullException( "cursorToken" );
            if( forToken == null ) throw new ArgumentNullException( "forToken" );
            if( select == null ) throw new ArgumentNullException( "select" );
            if( (forOptionsToken == null) != (updateToken == null) ) throw new ArgumentException( "Expected 'for update'.", "forToken" );
            if( (ofToken == null) != (updateColumns == null) ) throw new ArgumentException( "Required at least one column name after 'of' or no of and no columns.", "ofToken" );

            if( options != null )
            {
                if( forOptionsToken != null )
                {
                    if( ofToken != null ) return CreateArray( SqlToken.EmptyOpenPar, cursorToken, options, forToken, select, forOptionsToken, updateToken, ofToken, updateColumns, SqlToken.EmptyClosePar );
                    return CreateArray( SqlToken.EmptyOpenPar, cursorToken, options, forToken, select, forOptionsToken, updateToken, SqlToken.EmptyClosePar );
                }
                else
                {
                    return CreateArray( SqlToken.EmptyOpenPar, cursorToken, options, forToken, select, SqlToken.EmptyClosePar );
                }
            }
            else
            {
                if( forOptionsToken != null )
                {
                    if( ofToken != null ) return CreateArray( SqlToken.EmptyOpenPar, cursorToken, forToken, select, forOptionsToken, updateToken, ofToken, updateColumns, SqlToken.EmptyClosePar );
                    return CreateArray( SqlToken.EmptyOpenPar, cursorToken, forToken, select, forOptionsToken, updateToken, SqlToken.EmptyClosePar );
                }
                else
                {
                    return CreateArray( SqlToken.EmptyOpenPar, cursorToken, forToken, select, SqlToken.EmptyClosePar );
                }
            }
        }

        internal SqlExprCursor( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public bool IsSql92Syntax { get { return false; } }

        /// <summary>
        /// Gets the first case token.
        /// </summary>
        public SqlTokenIdentifier CursorT { get { return (SqlTokenIdentifier)Slots[1]; } }

        /// <summary>
        /// Gets the options (null if none). Can be [LOCAL|GLOBAL][FORWARD_ONLY|SCROLL][STATIC|KEYSET|DYNAMIC|FAST_FORWARD][READ_ONLY|SCROLL_LOCKS|OPTIMISTIC][TYPE_WARNING]. 
        /// </summary>
        public SqlExprUnmodeledItems Options { get { return Slots[2] as SqlExprUnmodeledItems; } }

        /// <summary>
        /// Gets the select specification for this cursor.
        /// </summary>
        public ISelectSpecification Select 
        {
            get { return Slots[3] as SelectSpecification ?? Slots[4] as ISelectSpecification; } 
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
