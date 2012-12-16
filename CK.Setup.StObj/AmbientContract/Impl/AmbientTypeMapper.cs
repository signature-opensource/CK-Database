using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;

namespace CK.Core
{
    internal class AmbientTypeMapper : IAmbientTypeMapper
    {
        readonly ListDictionary _contextMappers;
        readonly ContextCollection _contextsEx;

        class ContextCollection : IReadOnlyCollection<IAmbientTypeContextualMapper>
        {
            readonly AmbientTypeMapper _a;

            public ContextCollection( AmbientTypeMapper a )
            {
                _a = a;
            }

            public bool Contains( object item )
            {
                IAmbientTypeContextualMapper c = item as IAmbientTypeContextualMapper;
                return c != null ? c.Owner == _a : false;
            }

            public int Count
            {
                get { return _a._contextMappers.Count; }
            }

            public IEnumerator<IAmbientTypeContextualMapper> GetEnumerator()
            {
                return _a._contextMappers.Values.Cast<IAmbientTypeContextualMapper>().GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _a._contextMappers.Values.GetEnumerator();
            }
        }

        internal AmbientTypeMapper()
        {
            _contextMappers = new ListDictionary();
            _contextsEx = new ContextCollection( this );
        }
        
        public IAmbientTypeContextualMapper Default
        {
            get { return (IAmbientTypeContextualMapper)_contextMappers[String.Empty]; }
        }

        public IReadOnlyCollection<IAmbientTypeContextualMapper> Contexts { get { return _contextsEx; } }

        public IAmbientTypeContextualMapper FindContext( string context )
        {
            return (IAmbientTypeContextualMapper)_contextMappers[context ?? String.Empty];
        }


        internal void Add( AmbientTypeContextualMapper c )
        {
            Debug.Assert( c.Context != null );
            _contextMappers.Add( c.Context, c );
        }
    }
}
