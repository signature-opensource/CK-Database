using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public abstract class SqlExprBaseListWithSeparatorList<T> : SqlExprBaseListWithSeparator<T>, IReadOnlyList<T> where T : class, ISqlItem
    {
        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseListWithSeparatorList{T}"/> of <typeparamref name="T"/> enclosed in a <see cref="SqlTokenOpenPar"/> and a <see cref="SqlTokenClosePar"/> 
        /// and with <paramref name="validSeparator"/> that is to <see cref="IsCommaSeparator"/> by default.
        /// </summary>
        /// <param name="openPar">Opening parenthesis.</param>
        /// <param name="exprOrCommaTokens">List of tokens or expressions.</param>
        /// <param name="closePar">Closing parenthesis.</param>
        /// <param name="allowEmpty">False to throw an argument exception if the <paramref name="exprOrCommaTokens"/> is empty.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        public SqlExprBaseListWithSeparatorList( SqlTokenOpenPar openPar, IList<ISqlItem> exprOrTokens, SqlTokenClosePar closePar, bool allowEmpty, Predicate<ISqlItem> validSeparator = null )
            : base( openPar, exprOrTokens, closePar, allowEmpty, validSeparator )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseListWithSeparatorList{T}"/> of <typeparamref name="T"/> without <see cref="Opener"/> nor <see cref="Closer"/> 
        /// and with <paramref name="validSeparator"/> that is <see cref="IsCommaSeparator"/> by default.
        /// </summary>
        /// <param name="exprOrTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        public SqlExprBaseListWithSeparatorList( IList<ISqlItem> exprOrTokens, bool allowEmpty, Predicate<ISqlItem> validSeparator = null )
            : base( exprOrTokens, allowEmpty, validSeparator )
        {
        }

        internal SqlExprBaseListWithSeparatorList( ISqlItem[] components )
            : base( components )
        {
        }


        public int IndexOf( object item )
        {
            T t = item as T;
            return t != null ? NonSeparatorTokens.IndexOf( x => x == t ) : -1;
        }

        public T this[int index]
        {
            get { return NonSeparatorTokenAt( index ); }
        }

        public bool Contains( object item )
        {
            T t = item as T;
            return t != null ? NonSeparatorTokens.Contains( t ) : false;
        }

        /// <summary>
        /// Gets the number of <typeparam name="T"/> items in this list.
        /// </summary>
        public int Count
        {
            get { return NonSeparatorCount; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return NonSeparatorTokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

}
