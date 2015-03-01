#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\Select\SelectOrderByColumn.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Parser
{
    public class SelectOrderByColumn : SqlNoExpr
    {
        public SelectOrderByColumn( SqlExpr definition, SqlTokenIdentifier ascOrDesc = null )
            : base( ascOrDesc != null ? CreateArray( definition, ascOrDesc ) : CreateArray( definition ) )
        {
        }

        internal SelectOrderByColumn( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExpr Definition { get { return (SqlExpr)Slots[0]; } }

        /// <summary>
        /// Gets the 'asc' or 'desc' token. Null if not specified.
        /// </summary>
        public SqlTokenIdentifier AscOrDescT { get { return Slots.Length == 2 ? (SqlTokenIdentifier)Slots[1] : null; } }

        /// <summary>
        /// True if the <see cref="AscOrDescT"/> is not specified or if it is 'asc'.
        /// </summary>
        public bool IsAsc { get { return Slots.Length == 1 || ((SqlTokenIdentifier)Slots[1]).TokenType == SqlTokenType.Asc; } }
        
        /// <summary>
        /// True id 'desc' is specified.
        /// </summary>
        public bool IsDesc { get { return Slots.Length == 2 && ((SqlTokenIdentifier)Slots[1]).TokenType == SqlTokenType.Desc; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}