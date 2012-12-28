using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{
    internal class StObjMapper : IStObjMapper
    {
        readonly ListDictionary _contextMappers;
        readonly ContextCollection _contextsEx;

        class ContextCollection : IReadOnlyCollection<IStObjContextualMapper>
        {
            readonly StObjMapper _a;

            public ContextCollection( StObjMapper a )
            {
                _a = a;
            }

            public bool Contains( object item )
            {
                IStObjContextualMapper c = item as IStObjContextualMapper;
                return c != null ? c.Owner == _a : false;
            }

            public int Count
            {
                get { return _a._contextMappers.Count; }
            }

            public IEnumerator<IStObjContextualMapper> GetEnumerator()
            {
                return _a._contextMappers.Values.Cast<IStObjContextualMapper>().GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _a._contextMappers.Values.GetEnumerator();
            }
        }

        internal StObjMapper()
        {
            _contextMappers = new ListDictionary();
            _contextsEx = new ContextCollection( this );
        }
        
        public IStObjContextualMapper Default
        {
            get { return (IStObjContextualMapper)_contextMappers[String.Empty]; }
        }

        public IReadOnlyCollection<IStObjContextualMapper> Contexts 
        {
            get { return _contextsEx; } 
        }

        public IStObjContextualMapper FindContext(  string context )
        {
            return (IStObjContextualMapper)_contextMappers[context ?? String.Empty];
        }

        internal void Add( StObjContextualMapper c )
        {
            Debug.Assert( c.Context != null );
            _contextMappers.Add( c.Context, c );
        }
    }
}
