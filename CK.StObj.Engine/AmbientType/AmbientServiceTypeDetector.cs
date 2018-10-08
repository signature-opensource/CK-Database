using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.Core
{
    class AmbientServiceTypeDetector
    {
        readonly Dictionary<Type, ServiceLifetime> _cache;

        public AmbientServiceTypeDetector()
        {
            _cache = new Dictionary<Type, ServiceLifetime>();
        }

        /// <summary>
        /// Defines a type as being a pure <see cref="ServiceLifetime.IsSingleton"/>.
        /// Can be called multiple times as long as no different registration already exists.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>True on success, false on error.</returns>
        public bool DefineAsExternalSingleton( IActivityMonitor m, Type t )
        {
            return DefineAsExternal( m, t, ServiceLifetime.IsSingleton );
        }

        /// <summary>
        /// Defines a type as being a pure <see cref="ServiceLifetime.IsScoped"/>.
        /// Can be called multiple times as long as no different registration already exists.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>True on success, false on error.</returns>
        public bool DefineAsExternalScoped( IActivityMonitor m, Type t )
        {
            return DefineAsExternal( m, t, ServiceLifetime.IsScoped );
        }

        bool DefineAsExternal( IActivityMonitor m, Type t, ServiceLifetime lifetime )
        {
            if( _cache.TryGetValue( t, out var lt ) )
            {
                if( lt != lifetime )
                {
                    if( lt == ServiceLifetime.None )
                    {
                        _cache[t] = lifetime;
                        return true;
                    }
                    m.Error( $"Type '{t.Name}' is already registered with '{lt}' lifetime. It can not be defined as external {lifetime}." );
                    return false;
                }
                return true;
            }
            _cache.Add( t, lifetime );
            return true;
        }

        /// <summary>
        /// Checks whether the type has a IScopedAmbientService, ISingletonAmbientService
        /// interface (or <see cref="ServiceLifetime.BothError"/>) or IAmbientService or
        /// has been registered as a <see cref="ServiceLifetime.IsScoped"/>
        /// or <see cref="ServiceLifetime.IsSingleton"/>.
        /// Only the interface name matters (namespace is ignored) and the interface
        /// must be a pure marker, there must be no declared members.
        /// </summary>
        /// <param name="t">The type that can be an interface or a class.</param>
        /// <returns>The associated service lifetime.</returns>
        public ServiceLifetime GetAmbientServiceLifetime( Type t )
        {
            var k = RawGet( t );
            return (k & (ServiceLifetime)8) == 0 ? k : ServiceLifetime.None;
        }

        ServiceLifetime RawGet( Type t )
        {
            if( !_cache.TryGetValue( t, out var k ) )
            {
                var allInterfaces = t.GetInterfaces();
                if( t.IsInterface
                    && allInterfaces.Length <= 1
                    && t.GetMembers().Length == 0 )
                {
                    if( t.Name == nameof( IAmbientService ) ) k = ServiceLifetime.IsAmbientService | (ServiceLifetime)8;
                    else if( t.Name == nameof( IScopedAmbientService ) ) k = ServiceLifetime.AmbientScope | (ServiceLifetime)8;
                    else if( t.Name == nameof( ISingletonAmbientService ) ) k = ServiceLifetime.AmbientSingleton | (ServiceLifetime)8;
                    else if( allInterfaces.Length == 1 ) k |= RawGet( allInterfaces[0] ) & (ServiceLifetime)7;
                    _cache.Add( t, k );
                    return k;
                }
                foreach( var i in allInterfaces )
                {
                    k |= RawGet( i ) & (ServiceLifetime)7;
                }
                _cache.Add( t, k );
            }
            return k;
        }

    }

}
