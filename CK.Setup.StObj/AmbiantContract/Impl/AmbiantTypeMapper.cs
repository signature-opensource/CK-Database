using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;

namespace CK.Core
{
    internal class AmbiantTypeMapper : IAmbiantTypeMapper
    {
        ListDictionary _contextMappers;

        internal AmbiantTypeMapper()
        {
            _contextMappers = new ListDictionary();
        }
        
        public IAmbiantTypeContextualMapper Default
        {
            get { return (IAmbiantTypeContextualMapper)_contextMappers[AmbiantContractCollector.DefaultContext]; }
        }

        public IAmbiantTypeContextualMapper this[Type typedContext]
        {
            get { return (IAmbiantTypeContextualMapper)_contextMappers[typedContext ?? AmbiantContractCollector.DefaultContext]; }
        }

        public bool Contains( object item )
        {
            IAmbiantTypeContextualMapper c = item as IAmbiantTypeContextualMapper;
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
        public IEnumerator<IAmbiantTypeContextualMapper> GetEnumerator()
        {
            return _contextMappers.Values.Cast<IAmbiantTypeContextualMapper>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _contextMappers.Values.GetEnumerator();
        }

        internal void Add( AmbiantTypeContextualMapper c )
        {
            Debug.Assert( c.Context != null );
            _contextMappers.Add( c.Context, c );
        }
    }
}
