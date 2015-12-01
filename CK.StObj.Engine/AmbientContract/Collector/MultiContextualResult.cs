#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AmbientContract\Collector\MultiContextualResult.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
    /// At least one context exists: the <see cref="Default"/> one. 
    /// Extraneous contexts are identified by a non empty nor null string. 
    /// Use <see cref="FindContext"/> or <see cref="ContextCollection"/> to access them.
    /// </summary>
    public class MultiContextualResult<T>
        where T : class, IContextualResult
    {
        readonly ListDictionary _contextResults;
        readonly ContextCollection _contextsEx;

        class ContextCollection : IReadOnlyCollection<T>
        {
            readonly ListDictionary _c;

            public ContextCollection( ListDictionary c )
            {
                _c = c;
            }

            public bool Contains( object item )
            {
                T c = item as T;
                return c != null ? _c.Contains( c.Context ) : false;
            }

            public int Count
            {
                get { return _c.Count; }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _c.Values.Cast<T>().GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _c.Values.GetEnumerator();
            }
        }

        internal MultiContextualResult()
        {
            _contextResults = new ListDictionary();
            _contextsEx = new ContextCollection( _contextResults );
        }

        /// <summary>
        /// Gets the result for the default context (<see cref="String.Empty"/>).
        /// </summary>
        public T Default
        {
            get { return (T)_contextResults[String.Empty]; }
        }

        /// <summary>
        /// Gets the different contexts (including <see cref="Default"/>).
        /// </summary>
        public IReadOnlyCollection<T> Contexts { get { return _contextsEx; } }

        /// <summary>
        /// Gets the result for any context or null if no such context exist.
        /// </summary>
        /// <param name="context">Type that identifies a context (null is the same as <see cref="String.Empty"/>).</param>
        /// <returns>The result for the given context.</returns>
        public T FindContext( string context )
        {
            return (T)_contextResults[context ?? String.Empty];
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
                if( c.Context.Length > 0 ) action( c );
            }
        }

        internal T Add( T c )
        {
            Debug.Assert( c.Context != null );
            _contextResults.Add( c.Context, c );
            return c;
        }

    }
}
