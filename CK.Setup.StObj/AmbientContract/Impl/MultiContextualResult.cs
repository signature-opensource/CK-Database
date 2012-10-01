using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Utility class that encapsulates multiple context dependent objects of type <typeparam name="T"/> that implements <see cref="IContextualResult"/>.
    /// At least one context exists: the <see cref="Default"/> one. Extraneous contexts are defined and 
    /// identified by a <see cref="Type"/>. Use <see cref="Item(Type)">indexer</see> to access them.
    /// </summary>
    public class MultiContextualResult<T> : IReadOnlyCollection<T>
        where T : class, IContextualResult
    {
        ListDictionary _contextResults;

        internal MultiContextualResult()
        {
            _contextResults = new ListDictionary();
        }

        /// <summary>
        /// Gets the total number of contexts (at least one since <see cref="Default"/> is always available).
        /// </summary>
        public int Count
        {
            get { return _contextResults.Count; }
        }


        /// <summary>
        /// Gets the result for the default context (<see cref="AmbientContractCollector"/>.<see cref="AmbientContractCollector.DefaultContext"/>).
        /// </summary>
        public T Default
        {
            get { return (T)_contextResults[AmbientContractCollector.DefaultContext]; }
        }

        /// <summary>
        /// Gets the result for any context <see cref="Type"/> or null if no such context exist.
        /// </summary>
        /// <param name="context">Type that identifies a context (null is the same as <see cref="AmbientContractCollector.DefaultContext"/>).</param>
        /// <returns>The result for the given context.</returns>
        public T this[Type context]
        {
            get { return (T)_contextResults[context ?? AmbientContractCollector.DefaultContext]; }
        }

        /// <summary>
        /// Provides an enumerator for all results (including <see cref="Default"/>).
        /// </summary>
        /// <returns>Enumeration of result objects.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _contextResults.Values.Cast<T>().GetEnumerator();
        }

        /// <summary>
        /// Gets whether at least one result has a fatal error. 
        /// Can be overriden to take into account other errors.
        /// </summary>
        public virtual bool HasFatalError
        {
            get
            {
                foreach( T c in _contextResults.Values ) if( c.HasFatalError ) return true;
                return false;
            }
        }

        /// <summary>
        /// Submits <see cref="Default"/> and then other contexts context to the given <see cref="Action"/>.
        /// </summary>
        /// <param name="action">Action to apply to contexts.</param>
        protected void Foreach( Action<T> action )
        {
            if( action == null ) throw new ArgumentNullException( "action" );
            T cDef = Default;
            if( cDef != null ) action( cDef );
            foreach( T c in _contextResults.Values )
            {
                if( c.Context != AmbientContractCollector.DefaultContext ) action( c );
            }
        }

        internal T Add( T c )
        {
            Debug.Assert( c.Context != null );
            _contextResults.Add( c.Context, c );
            return c;
        }

        bool IReadOnlyCollection<T>.Contains( object item )
        {
            T c = item as T;
            return c != null ? _contextResults.Contains( c.Context ) : false;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _contextResults.Values.GetEnumerator();
        }

    }
}
