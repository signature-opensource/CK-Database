using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprParameterList : SqlExprBaseListWithSeparatorList<SqlExprParameter>
    {
        public SqlExprParameterList( IEnumerable<SqlToken> tokens )
            : base( tokens )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
