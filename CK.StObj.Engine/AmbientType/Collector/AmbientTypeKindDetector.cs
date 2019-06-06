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
    public class AmbientTypeKindDetector
    {
        const AmbientTypeKind MaskPublicInfo = (AmbientTypeKind)15;
        const AmbientTypeKind IsDefiner = (AmbientTypeKind)16;

        // The lifetime reason is the interface marker (applies to IsSingleton and IsScoped).
        const AmbientTypeKind IsReasonMarker = (AmbientTypeKind)32;
        // The lifetime reason is an external definition (applies to IsSingleton and IsScoped).
        const AmbientTypeKind IsReasonExternal = (AmbientTypeKind)64;
        // The type is singleton because it is used as a:
        // - ctor parameter of a Singleton Service.
        // - property or StObjConstruct/StObjFinalize parameter of an Ambient Object.
        const AmbientTypeKind IsSingletonReasonReference = (AmbientTypeKind)128;
        // The type is a singleton because nothing prevents it to be a singleton.
        const AmbientTypeKind IsSingletonReasonFinal = (AmbientTypeKind)256;
        // The type is a service that is scoped because its ctor references a scoped service.
        const AmbientTypeKind IsScopedReasonReference = (AmbientTypeKind)512;

        readonly Dictionary<Type, AmbientTypeKind> _cache;

        /// <summary>
        /// Initializes a new detector.
        /// </summary>
        public AmbientTypeKindDetector()
        {
            _cache = new Dictionary<Type, AmbientTypeKind>();
        }

        /// <summary>
        /// Gets whether a registered type is a known to be a singleton.
        /// </summary>
        /// <param name="t">The already registered type.</param>
        /// <returns>True if this is a singleton.</returns>
        public bool IsSingleton( Type t ) => _cache.TryGetValue( t, out var i ) && (i&AmbientTypeKind.IsSingleton) != 0;

        /// <summary>
        /// Defines a type as being a <see cref="AmbientTypeKind.IsSingleton"/>.
        /// Can be called multiple times as long as no different registration already exists.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>The type kind on success, null on error.</returns>
        public AmbientTypeKind? DefineAsExternalSingleton( IActivityMonitor m, Type t )
        {
            return SetLifeTime( m, t, AmbientTypeKind.IsSingleton | IsReasonExternal );
        }

        /// <summary>
        /// Defines a type as being a <see cref="AmbientTypeKind.IsSingleton"/> because it is used
        /// as a ctor parameter of a Singleton Service.
        /// Can be called multiple times as long as lifetime is Singleton.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>The type kind on success, null on error.</returns>
        public AmbientTypeKind? DefineAsSingletonReference( IActivityMonitor m, Type t )
        {
            return SetLifeTime( m, t, AmbientTypeKind.IsSingleton | IsSingletonReasonReference );
        }

        /// <summary>
        /// Defines a type as being a pure <see cref="AmbientTypeKind.IsScoped"/>.
        /// Can be called multiple times as long as no different registration already exists.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>The type kind on success, null on error.</returns>
        public AmbientTypeKind? DefineAsExternalScoped( IActivityMonitor m, Type t )
        {
            return SetLifeTime( m, t, AmbientTypeKind.IsScoped | IsReasonExternal );
        }

        /// <summary>
        /// Promotes a type to be a singleton: it is good to be a singleton (for performance reasons).
        /// This is acted at the end of the process of handling services once we know that nothing
        /// prevents a <see cref="IAmbientService"/> to be a singleton.
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <param name="t">The type to promote.</param>
        /// <returns>The type kind on success, null on error.</returns>
        public AmbientTypeKind? PromoteToSingleton( IActivityMonitor m, Type t )
        {
            return SetLifeTime( m, t, AmbientTypeKind.IsSingleton | IsSingletonReasonFinal );
        }



        AmbientTypeKind? SetLifeTime( IActivityMonitor m, Type t, AmbientTypeKind kind  )
        {
            Debug.Assert( (kind & IsDefiner) == 0
                          && (kind & (AmbientTypeKind.IsScoped | AmbientTypeKind.IsSingleton)) != 0
                          && (kind & (AmbientTypeKind.IsScoped | AmbientTypeKind.IsSingleton)) != (AmbientTypeKind.IsScoped | AmbientTypeKind.IsSingleton) );
            var k = RawGet( m, t );
            if( (k & IsDefiner) != 0 )
            {
                throw new Exception( $"Type '{t.Name}' is a Definer. It cannot be defined as {ToStringFull( kind )}." );
            }
            var kType = k & (AmbientTypeKind.IsScoped | AmbientTypeKind.IsSingleton);
            Debug.Assert( kType != (AmbientTypeKind.IsScoped | AmbientTypeKind.IsSingleton) );
            if( kType != AmbientTypeKind.None && kType != (kind & (AmbientTypeKind.IsScoped | AmbientTypeKind.IsSingleton)) )
            {
                m.Error( $"Type '{t.Name}' is already registered as a '{ToStringFull( k )}'. It can not be defined as {ToStringFull( kind )}." );
                return null;
            }
            k |= kind;
            _cache[t] = k;
            Debug.Assert( (k & IsDefiner) == 0 );
            return k & MaskPublicInfo;
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
        /// <param name="m">The monitor to use.</param>
        /// <param name="t">The type that can be an interface or a class.</param>
        /// <returns>The ambient kind (may be invalid).</returns>
        public AmbientTypeKind GetKind( IActivityMonitor m, Type t )
        {
            var k = RawGet( m, t );
            return (k & IsDefiner) == 0
                        ? k & MaskPublicInfo
                        : AmbientTypeKind.None;
        }

        AmbientTypeKind RawGet( IActivityMonitor m, Type t )
        {
            if( !_cache.TryGetValue( t, out var k ) )
            {
                var allInterfaces = t.GetInterfaces();
                // First handkes the pure interface that have no base interfaces and no members: this can be one of our marker interfaces.
                // We must also handle here interfaces that have one base because IScoped/SingletonAmbientService are extending IAmbientService...
                if( t.IsInterface
                    && allInterfaces.Length <= 1
                    && t.GetMembers().Length == 0 )
                {
                    if( t.Name == nameof( IAmbientObject ) ) k = AmbientTypeKind.AmbientObject | IsDefiner | IsReasonMarker;
                    else if( t.Name == nameof( IAmbientService ) ) k = AmbientTypeKind.IsAmbientService | IsDefiner | IsReasonMarker;
                    else if( t.Name == nameof( IScopedAmbientService ) ) k = AmbientTypeKind.AmbientScope | IsDefiner | IsReasonMarker;
                    else if( t.Name == nameof( ISingletonAmbientService ) ) k = AmbientTypeKind.AmbientSingleton | IsDefiner | IsReasonMarker;
                }
                if( k == AmbientTypeKind.None )
                {
                    foreach( var i in allInterfaces )
                    {
                        k |= RawGet( m, i ) & ~IsDefiner;
                    }
                }
                bool isDefiner = t.GetCustomAttributesData().Any( a => a.AttributeType.Name == typeof(AmbientDefinerAttribute).Name );
                if( isDefiner )
                {
                    if( k != AmbientTypeKind.None ) k |= IsDefiner;
                    else
                    {
                        m.Error( $"Attribute [AmbientDefiner] is defined on type '{t}' that is not an ambient type." );
                    }
                }
                _cache.Add( t, k );
            }
            return k;
        }

        static string ToStringFull( AmbientTypeKind t )
        {
            var c = (t & MaskPublicInfo).ToStringClear();
            if( (t&IsDefiner) !=0 ) c += " [IsDefiner]";
            if( (t & IsReasonMarker) != 0 ) c += " [Marker]";
            if( (t & IsReasonExternal) != 0 ) c += " [External]";
            if( (t & IsSingletonReasonReference) != 0 ) c += " [ReferencedBySingleton]";
            if( (t & IsSingletonReasonFinal) != 0 ) c += " [OpimizedAsSingleton]";
            if( (t & IsScopedReasonReference) != 0 ) c += " [UsesScoped]";
            return c;
        }
    }

}
