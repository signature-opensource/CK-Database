using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Detector for <see cref="AmbientTypeKind"/>.
    /// </summary>
    public class AmbientTypeKindDetector : IServiceLifetimeResult
    {
        const AmbientTypeKind FlagIsBase = (AmbientTypeKind)64;
        const AmbientTypeKind MaskBase = (AmbientTypeKind)63;

        readonly Dictionary<Type, AmbientTypeKind> _cache;
        readonly List<Type> _externallyDefinedSingletons;

        /// <summary>
        /// Initializes a new detector.
        /// </summary>
        public AmbientTypeKindDetector()
        {
            _cache = new Dictionary<Type, AmbientTypeKind>();
            _externallyDefinedSingletons = new List<Type>();
        }

        /// <summary>
        /// Gets whether a registered type is a singleton.
        /// </summary>
        /// <param name="t">The already registered type.</param>
        /// <returns>True if this is a singleton.</returns>
        public bool IsExternalSingleton( Type t ) => _cache.TryGetValue( t, out var i ) && i == AmbientTypeKind.IsSingleton;

        public IReadOnlyCollection<Type> ExternallyDefinedSingletons => _externallyDefinedSingletons;

        /// <summary>
        /// Defines a type as being a pure <see cref="AmbientTypeKind.IsSingleton"/>.
        /// Can be called multiple times as long as no different registration already exists.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>True on success, false on error.</returns>
        public bool DefineAsExternalSingleton( IActivityMonitor m, Type t )
        {
            return DefineAsExternal( m, t, AmbientTypeKind.IsSingleton );
        }

        /// <summary>
        /// Defines a type as being a pure <see cref="AmbientTypeKind.IsScoped"/>.
        /// Can be called multiple times as long as no different registration already exists.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>True on success, false on error.</returns>
        public bool DefineAsExternalScoped( IActivityMonitor m, Type t )
        {
            return DefineAsExternal( m, t, AmbientTypeKind.IsScoped );
        }

        bool DefineAsExternal( IActivityMonitor m, Type t, AmbientTypeKind kind )
        {
            if( _cache.TryGetValue( t, out var k ) )
            {
                if( k == kind ) return true;
                if( k == AmbientTypeKind.None )
                {
                    _cache[t] = kind;
                }
                else
                {
                    m.Error( $"Type '{t.Name}' is already registered as a '{k}'. It can not be defined as external {kind}." );
                    return false;
                }
            }
            else _cache.Add( t, kind );
            if( kind == AmbientTypeKind.IsSingleton )
            {
                _externallyDefinedSingletons.Add( t );
            }
            return true;
        }

        /// <summary>
        /// Checks whether the type supports a IAmbientService, IScopedAmbientService, ISingletonAmbientService
        /// or IAmbientObject interface or has been explicitly registered as a <see cref="AmbientTypeKind.IsScoped"/>
        /// or <see cref="AmbientTypeKind.IsSingleton"/>.
        /// <para>
        /// Only the interface name matters (namespace is ignored) and the interface
        /// must be a pure marker, there must be no declared members.
        /// </para>
        /// <para>
        /// The result can be <see cref="AmbientTypeKindExtension.IsNoneOrInvalid(AmbientTypeKind)"/>.
        /// </para>
        /// </summary>
        /// <param name="t">The type that can be an interface or a class.</param>
        /// <returns>The ambient kind (may be invalid).</returns>
        public AmbientTypeKind GetKind( Type t )
        {
            var k = RawGet( t );
            return (k & FlagIsBase) == 0 ? k : AmbientTypeKind.None;
        }

        AmbientTypeKind RawGet( Type t )
        {
            if( !_cache.TryGetValue( t, out var k ) )
            {
                var allInterfaces = t.GetInterfaces();
                bool isAbstract = allInterfaces.Any( i => i.IsGenericType
                                            && i.GetGenericTypeDefinition().Name == typeof( IAmbientDefiner<> ).Name
                                            && i.GetGenericArguments()[0] == t );
                if( t.IsInterface
                    && allInterfaces.Length <= 1
                    && t.GetMembers().Length == 0 )
                {
                    if( t.Name == nameof( IAmbientObject ) ) k = AmbientTypeKind.AmbientObject | FlagIsBase;
                    else if( t.Name == nameof( IAmbientService ) ) k = AmbientTypeKind.IsAmbientService | FlagIsBase;
                    else if( t.Name == nameof( IScopedAmbientService ) ) k = AmbientTypeKind.AmbientScope | FlagIsBase;
                    else if( t.Name == nameof( ISingletonAmbientService ) ) k = AmbientTypeKind.AmbientSingleton | FlagIsBase;
                    else if( allInterfaces.Length == 1 )
                    {
                        k = RawGet( allInterfaces[0] ) & MaskBase;
                        if( isAbstract ) k |= FlagIsBase;
                    }
                    _cache.Add( t, k );
                    return k;
                }
                foreach( var i in allInterfaces )
                {
                    k |= RawGet( i ) & MaskBase;
                }
                if( isAbstract ) k |= FlagIsBase;
                _cache.Add( t, k );
            }
            return k;
        }

    }

}
