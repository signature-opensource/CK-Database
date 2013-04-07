using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{

    public class SqlExprMultiIdentifier : SqlExprBaseMultiIdentifier, ISqlIdentifier
    {
        public SqlExprMultiIdentifier( IList<IAbstractExpr> tokens )
            : base( tokens )
        {
        }

        internal SqlExprMultiIdentifier( IAbstractExpr[] newComponents )
            : base( newComponents )
        {
        }

        public new SqlExprMultiIdentifier RemoveQuoteIfPossible( bool keepIfReservedKeyword )
        {
            IAbstractExpr[] c = base.RemoveQuoteIfPossible( keepIfReservedKeyword );
            return c != null ? new SqlExprMultiIdentifier( c ) : this;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        bool ISqlIdentifier.IsVariable
        {
            get { return false; }
        }

    }

}
