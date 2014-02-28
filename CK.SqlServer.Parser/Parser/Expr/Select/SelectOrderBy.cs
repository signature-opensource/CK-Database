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
        public SelectOrderBy( ISelectSpecification select, SqlTokenIdentifier orderToken, SqlTokenIdentifier byToken, SelectOrderByColumnList columns )
            : this( CreateArray( SqlToken.EmptyOpenPar, select, orderToken, byToken, columns, SqlToken.EmptyClosePar ) )
        {
        }

        public SelectOrderBy( ISelectSpecification select, SqlTokenIdentifier orderToken, SqlTokenIdentifier byToken, SelectOrderByColumnList columns, SelectOrderByOffset offset )
            : this( CreateArray( SqlToken.EmptyOpenPar, select, orderToken, byToken, columns, offset, SqlToken.EmptyClosePar ) )
        {
        }

        internal SelectOrderBy( ISqlItem[] items )
            : base( items )
        {
        }

        public ISelectSpecification Select { get { return (ISelectSpecification)Slots[1]; } }

        public SqlExpr SelectExpr { get { return (SqlExpr)Slots[1]; } }

        public SqlTokenIdentifier OrderToken { get { return (SqlTokenIdentifier)Slots[2]; } }
        
        public SqlTokenIdentifier ByToken { get { return (SqlTokenIdentifier)Slots[3]; } }

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
