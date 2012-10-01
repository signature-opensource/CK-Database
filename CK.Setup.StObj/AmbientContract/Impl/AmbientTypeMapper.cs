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
        ListDictionary _contextMappers;

        internal AmbientTypeMapper()
        {
            _contextMappers = new ListDictionary();
        }
        
        public IAmbientTypeContextualMapper Default
        {
            get { return (IAmbientTypeContextualMapper)_contextMappers[AmbientContractCollector.DefaultContext]; }
        }

        public IAmbientTypeContextualMapper this[Type typedContext]
        {
            get { return (IAmbientTypeContextualMapper)_contextMappers[typedContext ?? AmbientContractCollector.DefaultContext]; }
        }

        public bool Contains( object item )
        {
            IAmbientTypeContextualMapper c = item as IAmbientTypeContextualMapper;
            return c != null ? c.Owner == this : false;
        }

        public int Count
        {
            get { return _contextMappers.Count; }
        }

        /// <summary>
        /// Provides an enumerator for all typed context (including <see cref="Default"/>).
        /// </summary>
        /// <returns>Enumeration of <see cref=""/> objects.</returns>
        public IEnumerator<IAmbientTypeContextualMapper> GetEnumerator()
        {
            return _contextMappers.Values.Cast<IAmbientTypeContextualMapper>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _contextMappers.Values.GetEnumerator();
        }

        internal void Add( AmbientTypeContextualMapper c )
        {
            Debug.Assert( c.Context != null );
            _contextMappers.Add( c.Context, c );
        }
    }
}
