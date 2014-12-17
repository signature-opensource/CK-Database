#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprColumnList.cs) is part of CK-Database. 
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
    public class SqlExprColumnList : SqlExprBaseExprList<SqlExprIdentifier>
    {
        /// <summary>
        /// Initializes a new list of columns with optional enclosing parentheses.
        /// </summary>
        /// <param name="openPar">Opening parenthesis. Can not be null.</param>
        /// <param name="tokens">Comma separated list of <see cref="SqlExprIdentifier"/> (can not be empty).</param>
        /// <param name="closePar">Closing parenthesis. Can not be null.</param>
        public SqlExprColumnList( SqlTokenOpenPar openPar, IList<ISqlItem> tokens, SqlTokenClosePar closePar )
            : base( openPar, tokens, closePar, false )
        {
        }

        internal SqlExprColumnList( ISqlItem[] newComponents )
            : base( newComponents )
        {
            Debug.Assert( NonSeparatorCount > 0, "Column list must not be empty." );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
