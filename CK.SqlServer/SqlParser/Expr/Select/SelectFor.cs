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
    /// Select "For" operator.
    /// </summary>
    public class SelectFor : SqlExpr, ISelectSpecification
    {
        public SelectFor( ISelectSpecification select, SqlTokenTerminal forToken, SqlExpr content )
            : this( CreateArray( SqlToken.EmptyOpenPar, select, forToken, content, SqlToken.EmptyClosePar ) )
        {
        }

        internal SelectFor( ISqlItem[] items )
            : base( items )
        {
        }

        public ISelectSpecification Select { get { return (ISelectSpecification)Slots[1]; } }

        public SqlExpr SelectExpr { get { return (SqlExpr)Slots[1]; } }

        public SqlExpr ForExpression { get { return (SqlExpr)Slots[3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public SqlTokenType CombinationKind
        {
            get { return SqlTokenType.For; }
        }

        public SelectColumnList Columns
        {
            get { return Select.Columns; }
        }

    }


}
