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
    /// Literal numbers (including 0x... literal binary values) and strings (either N'unicode' or 'one-byte-char').
    /// See <see cref="SqlTokenBaseLiteral"/>.
    /// </summary>
    public class SqlExprLiteral : SqlExprBaseMonoToken<SqlTokenBaseLiteral>
    {
        public SqlExprLiteral( SqlTokenBaseLiteral t )
            : base( t )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }


    }


}
