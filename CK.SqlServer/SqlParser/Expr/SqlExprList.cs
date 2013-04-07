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
    public class SqlExprList : SqlExprBaseExprList<SqlExpr>
    {
        /// <summary>
        /// Initializes a new list of expressions with enclosing parentheses.
        /// </summary>
        /// <param name="openPar">Opening parenthesis. Can not be null.</param>
        /// <param name="tokens">Comma separated list of <see cref="SqlExpr"/> (possibly empty).</param>
        /// <param name="closePar">Closing parenthesis. Can not be null.</param>
        public SqlExprList( SqlTokenOpenPar openPar, IList<IAbstractExpr> tokens, SqlTokenClosePar closePar )
            : base( openPar, tokens, closePar, true )
        {
        }

        /// <summary>
        /// Initializes a new list of expressions without enclosing parentheses.
        /// </summary>
        /// <param name="tokens">Comma separated list of <see cref="SqlExpr"/> (possibly empty).</param>
        public SqlExprList( IList<IAbstractExpr> tokens )
            : base( tokens, true )
        {
        }

        internal SqlExprList( IAbstractExpr[] newComponents )
            : base( newComponents )
        {
        }

        public override bool CanEnclose
        {
            get { return true; }
        }

        public override ISqlExprEnclosable Enclose( SqlTokenOpenPar opener, SqlTokenClosePar closer )
        {
            return new SqlExprList( CreateArray( opener, closer ) );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
