using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Detector for <see cref="AutoRealTypeKind"/>.
    /// </summary>
    public class AutoRealTypeKindDetector
    {
        const AutoRealTypeKind MaskPublicInfo = (AutoRealTypeKind)15;
        const AutoRealTypeKind IsDefiner = (AutoRealTypeKind)16;

        // The lifetime reason is the interface marker (applies to IsSingleton and IsScoped).
        const AutoRealTypeKind IsReasonMarker = (AutoRealTypeKind)32;
        // The lifetime reason is an external definition (applies to IsSingleton and IsScoped).
        const AutoRealTypeKind IsReasonExternal = (AutoRealTypeKind)64;
        // The type is singleton because it is used as a:
        // - ctor parameter of a Singleton Service.
        // - property or StObjConstruct/StObjFinalize parameter of a Real Object.
        const AutoRealTypeKind IsSingletonReasonReference = (AutoRealTypeKind)128;
        // The type is a singleton because nothing prevents it to be a singleton.
        const AutoRealTypeKind IsSingletonReasonFinal = (AutoRealTypeKind)256;
        // The type is a service that is scoped because its ctor references a scoped service.
        const AutoRealTypeKind IsScopedReasonReference = (AutoRealTypeKind)512;

        readonly Dictionary<Type, AutoRealTypeKind> _cache;

        /// <summary>
        /// Initializes a new detector.
        /// </summary>
        public AutoRealTypeKindDetector()
        {
            _cache = new Dictionary<Type, AutoRealTypeKind>();
        }

        /// <summary>
        /// Gets whether a registered type is known to be a singleton.
        /// </summary>
        /// <param name="t">The already registered type.</param>
        /// <returns>True if this is a singleton.</returns>
        public bool IsSingleton( Type t ) => _cache.TryGetValue( t, out var i ) && (i&AutoRealTypeKind.IsSingleton) != 0;

        /// <summary>
        /// Defines a type as being a <see cref="AutoRealTypeKind.IsSingleton"/>.
        /// Can be called multiple times as long as no different registration already exists.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>The type kind on success, null on error.</returns>
        public AutoRealTypeKind? DefineAsExternalSingleton( IActivityMonitor m, Type t )
        {
            return SetLifeTime( m, t, AutoRealTypeKind.IsSingleton | IsReasonExternal );
        }

        /// <summary>
        /// Defines a type as being a <see cref="AutoRealTypeKind.IsSingleton"/> because it is used
        /// as a ctor parameter of a Singleton Service.
        /// Can be called multiple times as long as lifetime is Singleton.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>The type kind on success, null on error.</returns>
        public AutoRealTypeKind? DefineAsSingletonReference( IActivityMonitor m, Type t )
        {
            return SetLifeTime( m, t, AutoRealTypeKind.IsSingleton | IsSingletonReasonReference );
        }

        /// <summary>
        /// Defines a type as being a pure <see cref="AutoRealTypeKind.IsScoped"/>.
        /// Can be called multiple times as long as no different registration already exists.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="t">The type to register.</param>
        /// <returns>The type kind on success, null on error.</returns>
        public AutoRealTypeKind? DefineAsExternalScoped( IActivityMonitor m, Type t )
        {
            return SetLifeTime( m, t, AutoRealTypeKind.IsScoped | IsReasonExternal );
        }

        /// <summary>
        /// Promotes a type to be a singleton: it is good to be a singleton (for performance reasons).
        /// This is acted at the end of the process of handling services once we know that nothing
        /// prevents a <see cref="IAutoService"/> to be a singleton.
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <param name="t">The type to promote.</param>
        /// <returns>The type kind on success, null on error.</returns>
        public AutoRealTypeKind? PromoteToSingleton( IActivityMonitor m, Type t )
        {
            return SetLifeTime( m, t, AutoRealTypeKind.IsSingleton | IsSingletonReasonFinal );
        }

        AutoRealTypeKind? SetLifeTime( IActivityMonitor m, Type t, AutoRealTypeKind kind  )
        {
            Debug.Assert( (kind & IsDefiner) == 0
                          && (kind & (AutoRealTypeKind.IsScoped | AutoRealTypeKind.IsSingleton)) != 0
                          && (kind & (AutoRealTypeKind.IsScoped | AutoRealTypeKind.IsSingleton)) != (AutoRealTypeKind.IsScoped | AutoRealTypeKind.IsSingleton) );
            var k = RawGet( m, t );
            if( (k & IsDefiner) != 0 )
            {
                throw new Exception( $"Type '{t}' is a Definer. It cannot be defined as {ToStringFull( kind )}." );
            }
            var kType = k & (AutoRealTypeKind.IsScoped | AutoRealTypeKind.IsSingleton);
            Debug.Assert( kType != (AutoRealTypeKind.IsScoped | AutoRealTypeKind.IsSingleton) );
            if( kType != AutoRealTypeKind.None && kType != (kind & (AutoRealTypeKind.IsScoped | AutoRealTypeKind.IsSingleton)) )
            {
                m.Error( $"Type '{t}' is already registered as a '{ToStringFull( k )}'. It can not be defined as {ToStringFull( kind )}." );
                return null;
            }
            k |= kind;
            _cache[t] = k;
            Debug.Assert( (k & IsDefiner) == 0 );
            return k & MaskPublicInfo;
        }

        /// <summary>
        /// Checks whether the type supports a IAutoService, IScopedAutoService, ISingletonAutoService
        /// or IRealObject interface or has been explicitly registered as a <see cref="AutoRealTypeKind.IsScoped"/>
        /// or <see cref="AutoRealTypeKind.IsSingleton"/>.
        /// <para>
        /// Only the interface name matters (namespace is ignored) and the interface
        /// must be a pure marker, there must be no declared members.
        /// </para>
        /// <para>
        /// The result can be <see cref="AutoRealTypeKindExtension.IsNoneOrInvalid(AutoRealTypeKind)"/>.
        /// </para>
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <param name="t">The type that can be an interface or a class.</param>
        /// <returns>The ambient kind (may be invalid).</returns>
        public AutoRealTypeKind GetKind( IActivityMonitor m, Type t )
        {
            var k = RawGet( m, t );
            return (k & IsDefiner) == 0
                        ? k & MaskPublicInfo
                        : AutoRealTypeKind.None;
        }

        AutoRealTypeKind RawGet( IActivityMonitor m, Type t )
        {
            if( !_cache.TryGetValue( t, out var k ) )
            {
                var allInterfaces = t.GetInterfaces();
                // First handkes the pure interface that have no base interfaces and no members: this can be one of our marker interfaces.
                // We must also handle here interfaces that have one base because IScoped/SingletonAutoService are extending IAutoService...
                if( t.IsInterface
                    && allInterfaces.Length <= 1
                    && t.GetMembers().Length == 0 )
                {
                    if( t.Name == nameof( IRealObject ) ) k = AutoRealTypeKind.RealObject | IsDefiner | IsReasonMarker;
                    else if( t.Name == nameof( IAutoService ) ) k = AutoRealTypeKind.IsAutoService | IsDefiner | IsReasonMarker;
                    else if( t.Name == nameof( IScopedAutoService ) ) k = AutoRealTypeKind.AutoScoped | IsDefiner | IsReasonMarker;
                    else if( t.Name == nameof( ISingletonAutoService ) ) k = AutoRealTypeKind.AutoSingleton | IsDefiner | IsReasonMarker;
                }
                if( k == AutoRealTypeKind.None )
                {
                    foreach( var i in allInterfaces )
                    {
                        k |= RawGet( m, i ) & ~IsDefiner;
                    }
                }
                bool isDefiner = t.GetCustomAttributesData().Any( a => a.AttributeType.Name == typeof(AutoRealDefinerAttribute).Name );
                if( isDefiner )
                {
                    if( k != AutoRealTypeKind.None ) k |= IsDefiner;
                    else
                    {
                        m.Error( $"Attribute [AutoRealDefiner] is defined on type '{t}' that is not a IAutoService or IRealObject type." );
                    }
                }
                if( k != AutoRealTypeKind.None && !t.Assembly.IsDynamic && !(t.IsPublic || t.IsNestedPublic) )
                {
                    m.Error( $"Type '{t}' being '{(k&MaskPublicInfo).ToStringClear( t.IsClass )}' must be public." );
                }
                _cache.Add( t, k );
            }
            return k;
        }
        static string ToStringFull( AutoRealTypeKind t )
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
