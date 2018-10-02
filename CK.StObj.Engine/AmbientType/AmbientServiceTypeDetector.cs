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
        /// Checks whether the type has a IScopedAmbientService or ISingletonAmbientService
        /// interface (or <see cref="ServiceLifetime.BothError"/>).
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
                    if( t.Name == nameof( IAmbientService ) ) k = ServiceLifetime.Ambient | (ServiceLifetime)8;
                    else if( t.Name == nameof( IScopedAmbientService ) ) k = ServiceLifetime.Scope | (ServiceLifetime)8;
                    else if( t.Name == nameof( ISingletonAmbientService ) ) k = ServiceLifetime.Singleton | (ServiceLifetime)8;
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
