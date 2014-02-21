using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public class SqlExprParameterList : SqlExprBaseExprList<SqlExprParameter>
    {
        /// <summary>
        /// Initializes a new list of parameters with enclosing parenthesis.
        /// </summary>
        /// <param name="openPar">Opening parenthesis. Can not be null.</param>
        /// <param name="content">Comma separated list of <see cref="SqlExprParameter"/> (possibly empty).</param>
        /// <param name="closePar">Closing parenthesis. Can not be null.</param>
        public SqlExprParameterList( SqlTokenOpenPar openPar, IList<ISqlItem> content, SqlTokenClosePar closePar )
            : base( openPar, content, closePar, true )
        {
        }

        /// <summary>
        /// Initializes a new list of parameters without parenthesis.
        /// </summary>
        /// <param name="content">Comma separated list of <see cref="SqlExprParameter"/> (possibly empty).</param>
        public SqlExprParameterList( IList<ISqlItem> content )
            : base( content, true )
        {
        }

        internal SqlExprParameterList( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        /// <summary>
        /// Gets the comma separated parameter list without the trivias.
        /// </summary>
        /// <returns>A well formatted, clean, string.</returns>
        public string ToStringClean()
        {
            return String.Join( ", ", this.Select( p => p.ToStringClean() ) );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
