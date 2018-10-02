using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using CK.Setup;

namespace CK.Core
{
    partial class StObjObjectEngineMap : IStObjObjectEngineMap, IStObjMap, IStObjServiceMap
    {
        readonly Dictionary<Type, AmbientServiceClassInfo> _serviceMap;
        readonly ServiceMapTypeAdapter _exposedServiceMap;
        readonly Dictionary<Type, IStObjServiceFinalManualMapping> _serviceManualMap;
        readonly ServiceManualMapTypeAdapter _exposedManualServiceMap;
        readonly List<IStObjServiceFinalManualMapping> _serviceManualList;

        class ServiceMapTypeAdapter : IReadOnlyDictionary<Type, Type>
        {
            readonly Dictionary<Type, AmbientServiceClassInfo> _map;

            public ServiceMapTypeAdapter( Dictionary<Type, AmbientServiceClassInfo> map )
            {
                _map = map;
            }

            public Type this[Type key]
            {
                get
                {
                    if( !_map.TryGetValue( key, out var c ) ) return null;
                    return c.FinalType;
                }
            }
            public IEnumerable<Type> Keys => _map.Keys;

            public IEnumerable<Type> Values => _map.Values.Select( c => c.FinalType );

            public int Count => _map.Count;

            public bool ContainsKey( Type key ) => _map.ContainsKey( key );

            public IEnumerator<KeyValuePair<Type, Type>> GetEnumerator()
            {
                return _map.Select( kv => new KeyValuePair<Type, Type>( kv.Key, kv.Value.FinalType ) ).GetEnumerator();
            }

            public bool TryGetValue( Type key, out Type value )
            {
                value = null;
                if( !_map.TryGetValue( key, out var c ) ) return false;
                value = c.FinalType;
                return true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        class ServiceManualMapTypeAdapter : IReadOnlyDictionary<Type, IStObjServiceClassFactory>
        {
            readonly Dictionary<Type, IStObjServiceFinalManualMapping> _map;

            public ServiceManualMapTypeAdapter( Dictionary<Type, IStObjServiceFinalManualMapping> map )
            {
                _map = map;
            }

            public IStObjServiceClassFactory this[Type key] => _map[key];

            public IEnumerable<Type> Keys => _map.Keys;

            public IEnumerable<IStObjServiceClassFactory> Values => _map.Values;

            public int Count => _map.Count;

            public bool ContainsKey( Type key ) => _map.ContainsKey( key );

            public IEnumerator<KeyValuePair<Type, IStObjServiceClassFactory>> GetEnumerator()
            {
                return _map.Select( kv => new KeyValuePair<Type, IStObjServiceClassFactory>( kv.Key, kv.Value ) ).GetEnumerator();
            }

            public bool TryGetValue( Type key, out IStObjServiceClassFactory value )
            {
                value = null;
                if( !_map.TryGetValue( key, out var c ) ) return false;
                value = c;
                return true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Direct access to the mutable service mapping.
        /// </summary>
        internal Dictionary<Type, AmbientServiceClassInfo> ServiceSimpleMappings => _serviceMap;

        internal Dictionary<Type, IStObjServiceFinalManualMapping> ServiceManualMappings => _serviceManualMap;

        internal IReadOnlyList<IStObjServiceFinalManualMapping> ServiceManualList => _serviceManualList;

        class StObjServiceFinalManualMapping : IStObjServiceFinalManualMapping
        {
            readonly IStObjServiceClassFactoryInfo _c;

            public StObjServiceFinalManualMapping( int number, IStObjServiceClassFactoryInfo c )
            {
                Number = number;
                _c = c;
            }

            public int Number { get; }

            public Type ClassType => _c.ClassType;

            public IReadOnlyList<IStObjServiceParameterInfo> Assignments => _c.Assignments;

            public object CreateInstance( IServiceProvider provider )
            {
                return Create( provider, this, new Dictionary<IStObjServiceClassFactoryInfo,object>() );
            }

            static object Create( IServiceProvider provider, IStObjServiceClassFactoryInfo c, Dictionary<IStObjServiceClassFactoryInfo,object> cache )
            {
                if( !cache.TryGetValue( c, out var result ) )
                {
                    var ctor = c.GetSingleConstructor();
                    var parameters = ctor.GetParameters();
                    var values = new object[parameters.Length];
                    for( int i = 0; i < parameters.Length; ++i )
                    {
                        var p = parameters[i];
                        var mapped = c.Assignments.Where( a => a.Position == p.Position ).FirstOrDefault();
                        if( mapped == null )
                        {
                            values[i] = provider.GetService( p.ParameterType );
                        }
                        else
                        {
                            if( mapped.Value == null )
                            {
                                values[i] = null;
                            }
                            else if( mapped.IsEnumerated )
                            {
                                values[i] = mapped.Value.Select( v => provider.GetService( v ) ).ToArray();
                            }
                            else
                            {
                                values[i] = provider.GetService( mapped.Value[0] );
                            }
                        }
                    }
                    result = ctor.Invoke( values );
                    cache.Add( c, result );
                }
                return result;
            }
        }

        internal IStObjServiceFinalManualMapping CreateStObjServiceFinalManualMapping( IStObjServiceClassFactoryInfo c )
        {
            var r = new StObjServiceFinalManualMapping( _serviceManualList.Count + 1, c );
            _serviceManualList.Add( r );
            return r;
        }

        IStObjServiceMap IStObjMap.Services => this;

        IReadOnlyDictionary<Type, Type> IStObjServiceMap.SimpleMappings => _exposedServiceMap;

        IReadOnlyDictionary<Type, IStObjServiceClassFactory> IStObjServiceMap.ManualMappings => _exposedManualServiceMap;
    }
}
