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
        T _default;
        List<T> _others;
        readonly ContextCollection _contextsEx;

        class ContextCollection : IReadOnlyCollection<T>
        {
            readonly MultiContextualResult<T> _c;

            public ContextCollection(MultiContextualResult<T> c )
            {
                _c = c;
            }

            public int Count => (_c._default != null ? 1 : 0) + (_c._others != null ? _c._others.Count : 0);

            public IEnumerator<T> GetEnumerator()
            {
                if (_c._default != null ) yield return _c._default;
                if (_c._others != null) foreach (var c in _c._others) yield return c;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal MultiContextualResult()
        {
            _contextsEx = new ContextCollection( this );
        }

        /// <summary>
        /// Gets the result for the default context (<see cref="String.Empty"/>).
        /// </summary>
        public T Default => _default; 

        /// <summary>
        /// Gets the different contexts (including <see cref="Default"/>).
        /// </summary>
        public IReadOnlyCollection<T> Contexts => _contextsEx; 

        /// <summary>
        /// Gets the result for any context or null if no such context exist.
        /// </summary>
        /// <param name="context">Type that identifies a context (null is the same as <see cref="String.Empty"/>).</param>
        /// <returns>The result for the given context.</returns>
        public T FindContext( string context ) => string.IsNullOrEmpty(context) 
                                                    ? _default
                                                    : _others?.FirstOrDefault( c => c.Context == context );

        /// <summary>
        /// Gets whether at least one result has a fatal error. 
        /// Can be overriden to take into account other errors.
        /// </summary>
        public virtual bool HasFatalError =>  _default?.HasFatalError == true 
                                                || _others?.Any( c => c.HasFatalError == true ) == true;

        /// <summary>
        /// Submits <see cref="Default"/> and then other contexts context to the given <see cref="Action"/>.
        /// </summary>
        /// <param name="action">Action to apply to contexts.</param>
        protected void Foreach( Action<T> action )
        {
            if( action == null ) throw new ArgumentNullException( "action" );
            if(_default != null ) action(_default);
            if( _others != null )
                foreach ( T c in _others ) action( c );
        }

        internal T Add( T c )
        {
            Debug.Assert( c.Context != null );
            if( c.Context.Length == 0 )
            {
                Debug.Assert(_default == null);
                _default = c;
            }
            else
            {
                if( _others == null ) _others = new List<T>();
                Debug.Assert(_others.All(o => o.Context != c.Context));
                _others.Add(c);
            }
            return c;
        }

    }
}
