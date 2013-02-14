using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public abstract class SqlExprBaseListWithSeparatorList<T> : SqlExprBaseListWithSeparator<T>, IReadOnlyList<T> where T : class, IAbstractExpr
    {
        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseListWithSeparatorList"/> of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="tokens">List of tokens.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas.</param>
        public SqlExprBaseListWithSeparatorList( IEnumerable<IAbstractExpr> tokens, Predicate<IAbstractExpr> validSeparator = null )
            : base( tokens, validSeparator )
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
