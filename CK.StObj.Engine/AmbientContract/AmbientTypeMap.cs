using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Exposes a multi contextual type mapping.
    /// </summary>
    public class AmbientTypeMap<CT> : IContextualRoot<IContextualTypeMap>
        where CT : class, IContextualTypeMap
    {
        readonly ListDictionary _contextMappers;
        readonly ContextCollection _contextsEx;

        class ContextCollection : IReadOnlyCollection<CT>
        {
            readonly AmbientTypeMap<CT> _a;

            public ContextCollection( AmbientTypeMap<CT> a )
            {
                _a = a;
            }

            public bool Contains( object item )
            {
                CT c = item as CT;
                return c != null ? c.AllContexts == _a : false;
            }

            public int Count
            {
                get { return _a._contextMappers.Count; }
            }

            public IEnumerator<CT> GetEnumerator()
            {
                return _a._contextMappers.Values.Cast<CT>().GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _a._contextMappers.Values.GetEnumerator();
            }
        }

        public AmbientTypeMap()
        {
            _contextMappers = new ListDictionary();
            _contextsEx = new ContextCollection( this );
        }

        /// <summary>
        /// Gets the default type mapper, the one identified by <see cref="String.Empty"/>.
        /// </summary>
        public CT Default
        {
            get { return (CT)_contextMappers[String.Empty]; }
        }

        /// <summary>
        /// Gets the different contexts (including <see cref="Default"/>).
        /// </summary>
        public IReadOnlyCollection<CT> Contexts { get { return _contextsEx; } }

        /// <summary>
        /// Gets the result for any context or null if no such context exist.
        /// </summary>
        /// <param name="context">Type that identifies a context (null is the same as <see cref="String.Empty"/>).</param>
        /// <returns>The result for the given context.</returns>
        public CT FindContext( string context )
        {
            return (CT)_contextMappers[context ?? String.Empty];
        }

        internal CT CreateAndAddContext<T,TC>( IActivityLogger logger, string context )
            where T : AmbientTypeInfo
            where TC : AmbientContextualTypeInfo<T, TC>
        {
            Debug.Assert( context != null );
            var c = CreateContext<T,TC>( logger, context );
            _contextMappers.Add( c.Context, c );
            return (CT)c;
        }

        protected virtual IContextualTypeMap CreateContext<T, TC>( IActivityLogger logger, string context )
            where T : AmbientTypeInfo
            where TC : AmbientContextualTypeInfo<T, TC>
        {
            return (IContextualTypeMap)new AmbientContextualTypeMap<T, TC>( this, context );
        }

        #region IAmbientTypeMap Members

        IContextualTypeMap IContextualRoot<IContextualTypeMap>.Default
        {
            get { return Default; }
        }

        IReadOnlyCollection<IContextualTypeMap> IContextualRoot<IContextualTypeMap>.Contexts
        {
            get { return Contexts; }
        }

        IContextualTypeMap IContextualRoot<IContextualTypeMap>.FindContext( string context )
        {
            return FindContext( context );
        }

        #endregion
    }
}
