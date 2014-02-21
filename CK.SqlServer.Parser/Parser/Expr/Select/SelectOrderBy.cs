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
        public SelectOrderBy( ISelectSpecification select, SqlTokenIdentifier orderToken, SqlTokenIdentifier byToken, SqlExpr content )
            : this( CreateArray( SqlToken.EmptyOpenPar, select, orderToken, byToken, content, SqlToken.EmptyClosePar ) )
        {
        }

        internal SelectOrderBy( ISqlItem[] items )
            : base( items )
        {
        }

        public ISelectSpecification Select { get { return (ISelectSpecification)Slots[1]; } }

        public SqlExpr SelectExpr { get { return (SqlExpr)Slots[1]; } }

        public SqlExpr OrderByExpression { get { return (SqlExpr)Slots[4]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
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
