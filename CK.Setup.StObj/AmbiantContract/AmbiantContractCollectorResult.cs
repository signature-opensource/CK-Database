using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    class ReadOnlyDictionaryOnDictionary<T, TKey> : IReadOnlyUniqueKeyedCollection<T, TKey>
        {
            readonly Dictionary<TKey,T> _map;
            readonly Func<T,TKey> _keyer;

            public ReadOnlyDictionaryOnDictionary( Dictionary<TKey, T> map, Func<T, TKey> keySelector = null )
            {
                _map = map;
                _keyer = keySelector;
            }

            public bool Contains( TKey key )
            {
 	            return _map.ContainsKey( key );
            }

            public T GetByKey( TKey key, out bool exists )
            {
                T i;
                exists = _map.TryGetValue( key, out i );
 	            return i;
            }

            public bool Contains( object item )
            {
                if( item is TKey ) return _map.ContainsKey( (TKey)item );
                if( item is T )
                {
                    if( _keyer != null ) return _map.ContainsKey( _keyer( (T)item ) );
                    return _map.ContainsValue( (T)item );
                }
                return false;
            }

            public int  Count
            {
	            get { return _map.Count; }
            }

            public IEnumerator<T>  GetEnumerator()
            {
 	            return _map.Values.GetEnumerator();
            }

            System.Collections.IEnumerator  System.Collections.IEnumerable.GetEnumerator()
            {
 	            return _map.Values.GetEnumerator();
            }
        }

    public class AmbiantContractCollectorResult<T> : MultiContextualResult<AmbiantContractCollectorContextualResult<T>>
        where T : AmbiantTypeInfo
    {
        readonly AmbiantTypeMapper _mappings;
        readonly Dictionary<Type,T> _typeInfo;
        readonly IReadOnlyUniqueKeyedCollection<T, Type> _typeInfoEx;

        internal AmbiantContractCollectorResult( AmbiantTypeMapper mappings, Dictionary<Type, T> typeInfo )
        {
            _mappings = mappings;
            _typeInfo = typeInfo;
            _typeInfoEx = new ReadOnlyDictionaryOnDictionary<T, Type>( _typeInfo, a => a.Type );
        }

        /// <summary>
        /// Logs detailed information about discovered ambiant contracts for all discovered contexts.
        /// </summary>
        /// <param name="logger">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            using( logger.OpenGroup( LogLevel.Trace, "Ambiant Contract discovering: {0} context(s).", Count ) )
            {
                Foreach( r => r.LogErrorAndWarnings( logger ) );
            }
        }

        /// <summary>
        /// Gets an indexed collection of <typeparam name="T"/> that are <see cref="AmbiantTypeInfo"/>: this provides a central 
        /// mapping from ambiant classes to any associated information.
        /// </summary>
        public IReadOnlyUniqueKeyedCollection<T, Type> TypeInformation
        {
            get { return _typeInfoEx; }
        }

        /// <summary>
        /// Gets the type mapper for the multiple existing contexts.
        /// </summary>
        public IAmbiantTypeMapper Mappings
        {
            get { return _mappings; }
        }

    }
}
