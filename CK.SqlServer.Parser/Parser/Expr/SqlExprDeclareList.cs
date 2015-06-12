#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprParameterList.cs) is part of CK-Database. 
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
    public class SqlExprDeclareList : SqlExprBaseExprList<SqlExprDeclare>
    {
        /// <summary>
        /// Initializes a new list of variable declarations.
        /// </summary>
        /// <param name="content">Comma separated list of <see cref="SqlExprDeclare"/> (must not be empty).</param>
        public SqlExprDeclareList( IList<ISqlItem> content )
            : base( content, false )
        {
        }

        internal SqlExprDeclareList( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        /// <summary>
        /// Gets the comma separated declaration list without the trivias.
        /// </summary>
        /// <returns>A well formatted, clean, string.</returns>
        public string ToStringClean()
        {
            return String.Join( ", ", this.Select( p => p.ToStringClean() ) );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
