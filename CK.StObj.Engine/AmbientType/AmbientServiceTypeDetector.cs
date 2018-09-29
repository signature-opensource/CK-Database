using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.Core
{
    class AmbientServiceTypeDetector
    {
        readonly Dictionary<Type, byte> _cache;

        public AmbientServiceTypeDetector()
        {
            _cache = new Dictionary<Type, byte>();
        }

        /// <summary>
        /// Checks whether the type has a IAmbientService interface.
        /// Only the interface name matters (namespace is ignored) and the interface
        /// must be a pure marker, there must be no declared members.
        /// </summary>
        /// <param name="t">The type that can be an interface or a class.</param>
        /// <returns>True if this type is marked with a IAmbientService marker interface.</returns>
        public bool IsAmbientService( Type t )
        {
            return GetAmbientServiceMarker( t ) == 1;
        }

        byte GetAmbientServiceMarker( Type t )
        {
            if( !_cache.TryGetValue( t, out var b ) )
            {
                if( t.IsInterface && IsAmbientServiceInterface( t ) )
                {
                    _cache.Add( t, 2 );
                    return 2;
                }
                foreach( var i in t.GetInterfaces() )
                {
                    if( GetAmbientServiceMarker( i ) >= 1 )
                    {
                        _cache.Add( t, 1 );
                        return 1;
                    }
                }
                Debug.Assert( b == 0 );
                _cache.Add( t, 0 );
            }
            return b;
        }

        static bool IsAmbientServiceInterface( Type t )
        {
            Debug.Assert( t.IsInterface );
            return t.Name == nameof(IAmbientService) && t.GetMembers().Length == 0;
        }
    }
    
}
