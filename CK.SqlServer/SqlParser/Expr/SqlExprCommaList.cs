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
    /// Comma separated list of <see cref="SqlExpr"/> (possibly empty).
    /// </summary>
    public class SqlExprCommaList : SqlExprBaseExprList<SqlExpr>
    {
        /// <summary>
        /// Initializes a new list of expressions with enclosing parenthesis.
        /// </summary>
        /// <param name="openPar">Opening parenthesis. Can not be null.</param>
        /// <param name="tokens">Comma separated list of <see cref="SqlExpr"/> (possibly empty).</param>
        /// <param name="closePar">Closing parenthesis. Can not be null.</param>
        public SqlExprCommaList( SqlTokenOpenPar openPar, IList<ISqlItem> tokens, SqlTokenClosePar closePar )
            : base( openPar, tokens, closePar, true )
        {
        }

        /// <summary>
        /// Initializes a new list of expressions without enclosing parenthesis.
        /// </summary>
        /// <param name="tokens">Comma separated list of <see cref="SqlExpr"/> (possibly empty).</param>
        public SqlExprCommaList( IList<ISqlItem> tokens )
            : base( tokens, true )
        {
        }

        internal SqlExprCommaList( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
