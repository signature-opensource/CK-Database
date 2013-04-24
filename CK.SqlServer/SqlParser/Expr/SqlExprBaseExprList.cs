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
    /// Base class for comma separated list (possibly empty) of <typeparamref name="T"/> that are <see cref="SqlItem"/> optionally enclosed in parenthesis.
    /// As a <see cref="ISqlExprList{T}"/> it exposes its items as a <see cref="IReadOnlyList{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SqlExprBaseExprList<T> : SqlExprBaseListWithSeparatorList<T>, ISqlExprList<T> where T : SqlItem 
    {

        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseExprList{T}"/> of <typeparamref name="T"/> enclosed in a <see cref="SqlTokenOpenPar"/> and a <see cref="SqlTokenClosePar"/>.
        /// </summary>
        /// <param name="exprOrCommaTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="SqlToken.IsCommaSeparator"/>).</param>
        public SqlExprBaseExprList( SqlTokenOpenPar openPar, IList<ISqlItem> exprOrCommaTokens, SqlTokenClosePar closePar, bool allowEmpty )
            : base( openPar, exprOrCommaTokens, closePar, allowEmpty, SqlToken.IsCommaSeparator )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseExprList{T}"/> of <typeparamref name="T"/> without <see cref="Opener"/> nor <see cref="Closer"/> 
        /// and with <paramref name="validSeparator"/> that is <see cref="IsCommaSeparator"/> by default.
        /// </summary>
        /// <param name="exprOrCommaTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        public SqlExprBaseExprList( IList<ISqlItem> exprOrCommaTokens, bool allowEmpty )
            : base( exprOrCommaTokens, allowEmpty, SqlToken.IsCommaSeparator )
        {
        }

        internal SqlExprBaseExprList( ISqlItem[] components )
            : base( components )
        {
        }

        
    }

}
