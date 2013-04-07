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
    /// Base class for comma separated list (possibly empty) of <typeparamref name="T"/> that are <see cref="SqlExpr"/> optionally enclosed in parenthesis.
    /// As a <see cref="ISqlExprList{T}"/> it exposes its items as a <see cref="IReadOnlyList{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SqlExprBaseExprList<T> : SqlExprBaseListWithSeparatorList<T>, ISqlExprList<T> where T : SqlExpr 
    {
        ///// <summary>
        ///// Initializes a new <see cref="SqlExprBaseExprList{T}"/> of <typeparamref name="T"/> with an <see cref="Opener"/> and a <see cref="Closer"/>.
        ///// </summary>
        ///// <param name="exprOrCommasTokens">List of comma tokens or expressions.</param>
        ///// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        //public SqlExprBaseExprList( SqlExprMultiToken<SqlTokenOpenPar> opener, IList<IAbstractExpr> exprOrCommasTokens, SqlExprMultiToken<SqlTokenClosePar> closer, bool allowEmpty )
        //    : base( opener, exprOrCommasTokens, closer, allowEmpty, IsCommaSeparator )
        //{
        //}

        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseExprList{T}"/> of <typeparamref name="T"/> enclosed in a <see cref="SqlTokenOpenPar"/> and a <see cref="SqlTokenClosePar"/>.
        /// </summary>
        /// <param name="exprOrCommaTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        public SqlExprBaseExprList( SqlTokenOpenPar openPar, IList<IAbstractExpr> exprOrCommaTokens, SqlTokenClosePar closePar, bool allowEmpty )
            : base( openPar, exprOrCommaTokens, closePar, allowEmpty, IsCommaSeparator )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseExprList{T}"/> of <typeparamref name="T"/> without <see cref="Opener"/> nor <see cref="Closer"/> 
        /// and with <paramref name="validSeparator"/> that is <see cref="IsCommaSeparator"/> by default.
        /// </summary>
        /// <param name="exprOrCommaTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        public SqlExprBaseExprList( IList<IAbstractExpr> exprOrCommaTokens, bool allowEmpty )
            : base( exprOrCommaTokens, allowEmpty, IsCommaSeparator )
        {
        }

        internal SqlExprBaseExprList( IAbstractExpr[] components )
            : base( components )
        {
        }

        
    }

}
