#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\Select\SelectOrderBy.cs) is part of CK-Database. 
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
    ///  "Order by" operator.
    /// </summary>
    public class SelectOrderBy : SqlExpr, ISelectSpecification
    {
        public SelectOrderBy( ISelectSpecification select, SqlTokenIdentifier orderT, SqlTokenIdentifier byT, SelectOrderByColumnList columns )
            : this( CreateArray( SqlToken.EmptyOpenPar, select, orderT, byT, columns, SqlToken.EmptyClosePar ) )
        {
        }

        public SelectOrderBy( ISelectSpecification select, SqlTokenIdentifier orderT, SqlTokenIdentifier byT, SelectOrderByColumnList columns, SelectOrderByOffset offset )
            : this( CreateArray( SqlToken.EmptyOpenPar, select, orderT, byT, columns, offset, SqlToken.EmptyClosePar ) )
        {
        }

        internal SelectOrderBy( ISqlItem[] items )
            : base( items )
        {
        }

        public ISelectSpecification Select { get { return (ISelectSpecification)Slots[1]; } }

        public SqlExpr SelectExpr { get { return (SqlExpr)Slots[1]; } }

        public SqlTokenIdentifier OrderT { get { return (SqlTokenIdentifier)Slots[2]; } }
        
        public SqlTokenIdentifier ByT { get { return (SqlTokenIdentifier)Slots[3]; } }

        public SelectOrderByColumnList OrderByColumns { get { return (SelectOrderByColumnList)Slots[4]; } }

        public SelectOrderByOffset OffsetClause { get { return Slots.Length > 6 ? (SelectOrderByOffset)Slots[5] : null; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public SqlTokenType CombinationKind
        {
            get { return SqlTokenType.Order; }
        }

        public SelectColumnList Columns
        {
            get { return Select.Columns; }
        }
    }


}
