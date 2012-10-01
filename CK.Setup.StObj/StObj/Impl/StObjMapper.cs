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
        ListDictionary _contextMappers;

        internal StObjMapper()
        {
            _contextMappers = new ListDictionary();
        }
        
        public IStObjContextualMapper Default
        {
            get { return (IStObjContextualMapper)_contextMappers[AmbientContractCollector.DefaultContext]; }
        }

        public IStObjContextualMapper this[Type typedContext]
        {
            get { return (IStObjContextualMapper)_contextMappers[typedContext ?? AmbientContractCollector.DefaultContext]; }
        }

        public bool Contains( object item )
        {
            IStObjContextualMapper c = item as IStObjContextualMapper;
            return c != null ? c.Owner == this : false;
        }

        public int Count
        {
            get { return _contextMappers.Count; }
        }

        /// <summary>
        /// Provides an enumerator for all mappers (including <see cref="Default"/>).
        /// </summary>
        /// <returns>Enumeration of <see cref="IStObjContextualMapper"/> objects.</returns>
        public IEnumerator<IStObjContextualMapper> GetEnumerator()
        {
            return _contextMappers.Values.Cast<IStObjContextualMapper>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _contextMappers.Values.GetEnumerator();
        }

        internal void Add( StObjContextualMapper c )
        {
            Debug.Assert( c.Context != null );
            _contextMappers.Add( c.Context, c );
        }
    }
}
