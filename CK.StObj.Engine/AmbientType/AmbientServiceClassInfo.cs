using CK.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Represents a service class/implementation.
    /// </summary>
    public class AmbientServiceClassInfo : AmbientTypeInfo, IStObjServiceClassDescriptor
    {
        HashSet<AmbientServiceClassInfo> _ctorParmetersClosure;
        // When not null, this contains the constructor parameters that must be singletons
        // for this service to be a singleton.
        List<ParameterInfo> _requiredParametersToBeSingletons;
        bool? _ctorBinding;

        /// <summary>
        /// Constructor parameter info: either a <see cref="AmbientServiceClassInfo"/>,
        /// <see cref="AmbientServiceInterfaceInfo"/> or a enumeration of one of them.
        /// </summary>
        public class CtorParameter
        {
            /// <summary>
            /// The parameter info.
            /// </summary>
            public readonly ParameterInfo ParameterInfo;

            /// <summary>
            /// Not null if this parameter is a service class (ie. an implementation).
            /// </summary>
            public readonly AmbientServiceClassInfo ServiceClass;

            /// <summary>
            /// Not null if this parameter is a service interface.
            /// </summary>
            public readonly AmbientServiceInterfaceInfo ServiceInterface;

            /// <summary>
            /// Currently unused.
            /// </summary>
            public readonly AmbientServiceClassInfo EnumeratedServiceClass;

            /// <summary>
            /// Currently unused.
            /// </summary>
            public readonly AmbientServiceInterfaceInfo EnumeratedServiceInterface;

            /// <summary>
            /// Gets the (unwrapped) Type of this parameter.
            /// When <see cref="IsEnumerated"/> is true, this is the type of the enumerated object:
            /// for IReadOnlyList&lt;X&gt;, this is typeof(X).
            /// </summary>
            Type ParameterType { get; }

            /// <summary>
            /// Gets whether this is an enumerable of class or interface.
            /// </summary>
            public bool IsEnumerated { get; }

            /// <summary>
            /// Gets the zero-based position of the parameter in the parameter list.
            /// </summary>
            public int Position => ParameterInfo.Position;

            /// <summary>
            /// Gets the name of the parameter.
            /// </summary>
            public string Name => ParameterInfo.Name;

            internal CtorParameter(
                ParameterInfo p,
                AmbientServiceClassInfo cS,
                AmbientServiceInterfaceInfo iS,
                bool isEnumerable )
            {
                Debug.Assert( (cS != null) ^ (iS != null) );
                ParameterInfo = p;
                ParameterType = cS?.Type ?? iS.Type;
                if( isEnumerable )
                {
                    IsEnumerated = true;
                    EnumeratedServiceClass = cS;
                    EnumeratedServiceInterface = iS;
                }
                else
                {
                    ServiceClass = cS;
                    ServiceInterface = iS;
                }
            }

            /// <summary>
            /// Overridden to return a readable string.
            /// </summary>
            /// <returns>A readable string.</returns>
            public override string ToString()
            {
                var typeName = ParameterInfo.Member.DeclaringType.Name;
                return $"{typeName}( {ParameterInfo.ParameterType.Name} {ParameterInfo.Name} {(ParameterInfo.HasDefaultValue ? "= null" : "")})";
            }
        }

        internal AmbientServiceClassInfo(
            IActivityMonitor m,
            IServiceProvider serviceProvider,
            AmbientServiceClassInfo parent,
            Type t,
            AmbientTypeCollector collector,
            bool isExcluded,
            AmbientTypeKind lifetime )
            : base( m, parent, t, serviceProvider, isExcluded )
        {
            Debug.Assert( Generalization == parent );
            Debug.Assert( lifetime == AmbientTypeKind.IsAmbientService
                          || lifetime == AmbientTypeKind.AmbientSingleton
                          || lifetime == AmbientTypeKind.AmbientScope );
            DeclaredLifetime = lifetime;
            if( lifetime == AmbientTypeKind.AmbientScope ) MustBeScopedLifetime = true;
            if( parent != null ) SpecializationDepth = parent.SpecializationDepth + 1;
            if( IsExcluded ) return;

            // AmbientServiceAttribute is currently not used. This is to associate a service
            // to a StObj package and may be useful for Service Unification support.
            //var aC = t.GetCustomAttribute<AmbientServiceAttribute>();
            //if( aC == null )
            //{
            //    m.Warn( $"Missing {nameof( AmbientServiceAttribute )} on '{t.FullName}'." );
            //}
            //else
            //{
            //    ContainerType = aC.Container;
            //    if( ContainerType == null )
            //    {
            //        m.Info( $"{nameof( AmbientServiceAttribute )} on '{t.FullName}' indicates no container." );
            //    }
            //}
        }

        /// <summary>
        /// Gets this Service class life time.
        /// This reflects the <see cref="IAmbientService"/> or <see cref="ISingletonAmbientService"/>
        /// vs. <see cref="IScopedAmbientService"/> interface marker.
        /// This can never be <see cref="AmbientTypeKindExtension.IsNoneOrInvalid(AmbientTypeKind)"/> since
        /// in such cases, the AmbientServiceClassInfo is not instanciated.
        /// </summary>
        public AmbientTypeKind DeclaredLifetime { get; }

        /// <summary>
        /// Gets whether this class must be <see cref="AmbientTypeKind.IsScoped"/> because of its dependencies.
        /// If its <see cref="DeclaredLifetime"/> is <see cref="AmbientTypeKind.IsSingleton"/> an error is detected
        /// either at the very beginning of the process based on the static parameter type information or at the
        /// end of the process when class and interface mappings are about to be resolved.
        /// </summary>
        public bool? MustBeScopedLifetime { get; private set; }

        /// <summary>
        /// Gets the generalization of this <see cref="Type"/>, it is be null if no base class exists.
        /// This property is valid even if this type is excluded (however this AmbientServiceClassInfo does not
        /// appear in generalization's <see cref="Specializations"/>).
        /// </summary>
        public new AmbientServiceClassInfo Generalization => (AmbientServiceClassInfo)base.Generalization;

        /// <summary>
        /// Gets the different specialized <see cref="AmbientServiceClassInfo"/> that are not excluded.
        /// </summary>
        /// <returns>An enumerable of <see cref="AmbientServiceClassInfo"/> that specialize this one.</returns>
        public new IEnumerable<AmbientServiceClassInfo> Specializations => base.Specializations.Cast<AmbientServiceClassInfo>();

        /// <summary>
        /// Gets the most specialized concrete (or abstract but auto implementable) implementation.
        /// This is available only once <see cref="AmbientTypeCollector.GetResult"/> has been called.
        /// As long as <see cref="AmbientServiceCollectorResult.HasFatalError"/> is false, this is never null
        /// since it can be this instance itself.
        /// </summary>
        public AmbientServiceClassInfo MostSpecialized { get; private set; }

        /// <summary>
        /// Gets the supported service interfaces.
        /// This is not null only if <see cref="IsIncluded"/> is true (ie. this class is not excluded
        /// and is on a concrete path) and may be empty if there is no service interface (the
        /// implementation itself is marked with any <see cref="IScopedAmbientService"/> marker).
        /// </summary>
        public IReadOnlyList<AmbientServiceInterfaceInfo> Interfaces { get; private set; }

        /// <summary>
        /// Gets the container type to which this service is associated.
        /// This can be null (service is considered to reside in the final package) or
        /// if an error occured.
        /// For Service Chaining Resolution to be available (either to depend on or be used by others),
        /// services must be associated to one container.
        /// </summary>
        public Type ContainerType { get; }

        /// <summary>
        /// Gets the StObj container.
        /// </summary>
        public IStObjResult Container => ContainerItem;

        internal MutableItem ContainerItem { get; private set; }

        /// <summary>
        /// Gets the constructor. This may be null if any error occurred.
        /// </summary>
        public ConstructorInfo ConstructorInfo { get; private set; }

        /// <summary>
        /// Gets the constructor parameters that we need to consider.
        /// Parameters that are not <see cref="IAmbientService"/> do not appear here.
        /// </summary>
        public IReadOnlyList<CtorParameter> ConstructorParameters { get; private set; }

        /// <summary>
        /// Gets the <see cref="ImplementableTypeInfo"/> if this <see cref="AmbientTypeInfo.Type"/>
        /// is abstract, null otherwise.
        /// </summary>
        public ImplementableTypeInfo ImplementableTypeInfo { get; private set; }

        /// <summary>
        /// Gets the final type that must be used: it is <see cref="ImplementableTypeInfo.StubType"/>
        /// if this type is abstract otherwise it is the associated concrete <see cref="AmbientTypeInfo.Type"/>.
        /// </summary>
        public Type FinalType => ImplementableTypeInfo?.StubType ?? Type;

        /// <summary>
        /// Gets the specialization depth from the first top AmbientServiceClassInfo.
        /// This is not the same as <see cref="AmbientObjectClassInfo.SpecializationDepth"/> that
        /// is relative to <see cref="Object"/> type.
        /// </summary>
        public int SpecializationDepth { get; }

        /// <summary>
        /// Gets whether this class is on a concrete path: it is not excluded and is not abstract
        /// or has at least one concrete specialization.
        /// Only included classes eventually participate to the setup process.
        /// </summary>
        public bool IsIncluded => Interfaces != null;

        internal void FinalizeMostSpecializedAndCollectSubGraphs( List<AmbientServiceClassInfo> subGraphCollector )
        {
            Debug.Assert( IsIncluded );
            if( MostSpecialized == null ) MostSpecialized = this;
            foreach( var s in Specializations )
            {
                if( s.MostSpecialized != MostSpecialized ) subGraphCollector.Add( s );
                s.FinalizeMostSpecializedAndCollectSubGraphs( subGraphCollector );
            }
        }

        /// <summary>
        /// This mimics the <see cref="AmbientObjectClassInfo.CreateMutableItemsPath"/> method
        /// to reproduce the exact same Type handling between Services and StObj (ignoring agstract tails
        /// for instance).
        /// This is simpler here since there is no split in type info (no MutableItem layer).
        /// </summary>
        internal bool InitializePath(
                        IActivityMonitor monitor,
                        AmbientTypeCollector collector,
                        AmbientServiceClassInfo generalization,
                        IDynamicAssembly tempAssembly,
                        List<AmbientServiceClassInfo> lastConcretes,
                        ref List<Type> abstractTails )
        {
            Debug.Assert( tempAssembly != null );
            Debug.Assert( !IsExcluded && ImplementableTypeInfo == null );
            Debug.Assert( Interfaces == null );
            bool isConcretePath = false;
            foreach( AmbientServiceClassInfo c in Specializations )
            {
                Debug.Assert( !c.IsExcluded );
                isConcretePath |= c.InitializePath( monitor, collector, this, tempAssembly, lastConcretes, ref abstractTails );
            }
            if( !isConcretePath )
            {
                if( Type.IsAbstract
                    && (ImplementableTypeInfo = CreateAbstractTypeImplementation( monitor, tempAssembly )) == null )
                {
                    if( abstractTails == null ) abstractTails = new List<Type>();
                    abstractTails.Add( Type );
                    Generalization?.RemoveSpecialization( this );
                }
                else
                {
                    isConcretePath = true;
                    lastConcretes.Add( this );
                }
            }
            if( isConcretePath )
            {
                // Only if this class IsIncluded: assigns the set of interfaces.
                // This way only interfaces that are actually used are registered in the collector.
                // An unused Ambient Service interface (ie. that has no implementation in the context)
                // is like any other interface.
                Interfaces = collector.RegisterServiceInterfaces( Type.GetInterfaces() ).ToArray();
            }
            return isConcretePath;
        }

        /// <summary>
        /// Sets one of the leaves of this class to be the most specialized one from this
        /// instance potentially up to the leaf (and handles container binding at the same time).
        /// At least one assignment (the one of this instance) is necessarily done.
        /// Trailing path may have already been resolved to this or to another specialization:
        /// classes that are already assigned are skipped.
        /// This must obviously be called bottom-up the inheritance chain.
        /// </summary>
        internal bool SetMostSpecialized(
            IActivityMonitor monitor,
            StObjObjectEngineMap engineMap,
            AmbientServiceClassInfo mostSpecialized )
        {
            Debug.Assert( IsIncluded );
            Debug.Assert( MostSpecialized == null );
            Debug.Assert( mostSpecialized != null && mostSpecialized.IsIncluded );
            Debug.Assert( !mostSpecialized.IsSpecialized );

            bool success = true;
#if DEBUG
            bool atLeastOneAssignment = false;
#endif
            var child = mostSpecialized;
            do
            {
                if( child.MostSpecialized == null )
                {
                    // Child's most specialized class has not been assigned yet: its generalization
                    // has not been assigned yet.
                    Debug.Assert( child.Generalization?.MostSpecialized == null );
                    child.MostSpecialized = mostSpecialized;
#if DEBUG
                    atLeastOneAssignment = true;
#endif
                    if( child.ContainerType != null )
                    {
                        if( (child.ContainerItem = engineMap.ToHighestImpl( child.ContainerType )) == null )
                        {
                            monitor.Error( $"Unable to resolve container '{child.ContainerType.FullName}' for service '{child.Type.FullName}' to a StObj." );
                            success = false;
                        }
                    }
                }
            }
            while( (child = child.Generalization) != Generalization );
#if DEBUG
            Debug.Assert( atLeastOneAssignment );
#endif
            return success;
        }

        /// <summary>
        /// Gets the parameters closure (including "Inheritance Constructor Parameters rule" and
        /// external intermediate classes).
        /// </summary>
        public HashSet<AmbientServiceClassInfo> ComputedCtorParametersClassClosure
        {
            get
            {
                Debug.Assert( _ctorParmetersClosure != null && _ctorBinding == true );
                return _ctorParmetersClosure;
            }
        }

        Type IStObjServiceClassDescriptor.ClassType => FinalType;

        bool IStObjServiceClassDescriptor.IsScoped => MustBeScopedLifetime.Value;

        /// <summary>
        /// Ensures that the final lifetime is computed: <see cref="MustBeScopedLifetime"/> will not be null
        /// once called.
        /// Returns the MustBeScopedLifetime (true if this Service implementation must be scoped and false for singleton).
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <param name="typeKindDetector">The type detector (used to check singleton life times and promote mere IAmbientService to singletons).</param>
        /// <param name="success">Success reference token.</param>
        /// <returns>True for scoped, false for singleton.</returns>
        internal bool GetFinalMustBeScopedLifetime( IActivityMonitor m, AmbientTypeKindDetector typeKindDetector, ref bool success )
        {
            if( !MustBeScopedLifetime.HasValue )
            {
                Debug.Assert( (DeclaredLifetime & AmbientTypeKind.IsAmbientService) != 0 );
                foreach( var p in ConstructorParameters )
                {
                    var c = p.ServiceClass?.MostSpecialized ?? p.ServiceInterface?.FinalResolved;
                    if( c != null )
                    {
                        if( c.GetFinalMustBeScopedLifetime( m, typeKindDetector, ref success ) )
                        {
                            if( DeclaredLifetime == AmbientTypeKind.AmbientSingleton )
                            {
                                m.Error( $"Lifetime error: Type '{Type.Name}' is {nameof( ISingletonAmbientService )} but parameter '{p.Name}' of type '{p.ParameterInfo.ParameterType.Name}' in constructor is Scoped." );
                                success = false;
                            }
                            if( !MustBeScopedLifetime.HasValue )
                            {
                                m.Info( $"Type '{Type.Name}' must be Scoped since parameter '{p.Name}' of type '{p.ParameterInfo.ParameterType.Name}' in constructor is Scoped." );
                            }
                            MustBeScopedLifetime = true;
                        }
                    }
                }
                if( !MustBeScopedLifetime.HasValue )
                {
                    if( _requiredParametersToBeSingletons != null )
                    {
                        Debug.Assert( DeclaredLifetime == AmbientTypeKind.IsAmbientService );
                        foreach( var external in _requiredParametersToBeSingletons )
                        {
                            if( !typeKindDetector.IsSingleton( external.ParameterType ) )
                            {
                                m.Info( $"Type '{Type.Name}' must be Scoped since parameter '{external.Name}' of type '{external.ParameterType.Name}' in constructor is not a Singleton." );
                                MustBeScopedLifetime = true;
                                break;
                            }
                        }
                    }
                    if( !MustBeScopedLifetime.HasValue )
                    {
                        MustBeScopedLifetime = false;
                        if( DeclaredLifetime != AmbientTypeKind.AmbientSingleton )
                        {
                            m.Info( $"Nothing prevents the class '{Type.Name}' to be a Singleton: this is the most efficient choice." );
                            success &= typeKindDetector.PromoteToSingleton( m, Type ) != null;
                        }
                    }
                }
            }
            return MustBeScopedLifetime.Value;
        }

        internal HashSet<AmbientServiceClassInfo> GetCtorParametersClassClosure(
            IActivityMonitor m,
            AmbientTypeCollector collector,
            ref bool initializationError )
        {
            if( _ctorParmetersClosure == null )
            {
                // Parameters of base classes are by design added to parameters of this instance.
                // This ensure the "Inheritance Constructor Parameters rule", even if parameters are
                // not exposed from the inherited constructor (and base parameters are direclty new'ed).
                _ctorParmetersClosure = new HashSet<AmbientServiceClassInfo>();
                if( Generalization != null )
                {
                    _ctorParmetersClosure.AddRange( Generalization.GetCtorParametersClassClosure( m, collector, ref initializationError ) );
                }
                if( !(initializationError |= !EnsureCtorBinding( m, collector ) ) )
                {
                    var replacedTargets = GetReplacedTargetsFromReplaceServiceAttribute( m, collector );
                    foreach( var cS in ConstructorParameters.Select( p => p.ServiceClass )
                                                           .Where( p => p != null )
                                                           .Concat( replacedTargets ) )
                    {
                        AmbientServiceClassInfo c = cS;
                        do { _ctorParmetersClosure.Add( c ); } while( (c = c.Generalization) != null );
                        var cParams = cS.GetCtorParametersClassClosure( m, collector, ref initializationError );
                        _ctorParmetersClosure.UnionWith( cParams );
                    }
                }
            }
            return _ctorParmetersClosure;
        }

        IEnumerable<AmbientServiceClassInfo> GetReplacedTargetsFromReplaceServiceAttribute( IActivityMonitor m, AmbientTypeCollector collector )
        {
            foreach( var p in Type.GetCustomAttributesData()
                                  .Where( a => a.AttributeType.Name == nameof( ReplaceAmbientServiceAttribute ) )
                                  .SelectMany( a => a.ConstructorArguments ) )
            {
                Type replaced;
                if( p.Value is string s )
                {
                    replaced = SimpleTypeFinder.WeakResolver( s, false );
                    if( replaced == null )
                    {
                        m.Warn( $"[ReplaceAmbientService] on type '{Type.Name}': the assembly qualified name '{s}' cannot be resolved. It is ignored." );
                        continue;
                    }
                }
                else
                {
                    replaced = p.Value as Type;
                    if( replaced == null )
                    {
                        m.Warn( $"[ReplaceAmbientService] on type '{Type.Name}': the parameter '{p.Value}' is not a Type. It is ignored." );
                        continue;
                    }
                }
                var target = collector.FindServiceClassInfo( replaced );
                if( target == null )
                {
                    m.Warn( $"[ReplaceAmbientService({replaced.Name})] on type '{Type.Name}': the Type to replace is not an Abienst Service class implementation. It is ignored." );
                }
                else
                {
                    yield return target;
                }
            }
        }

        internal bool EnsureCtorBinding( IActivityMonitor m, AmbientTypeCollector collector )
        {
            Debug.Assert( IsIncluded );
            if( _ctorBinding.HasValue ) return _ctorBinding.Value;
            bool success = false;
            var ctors = Type.GetConstructors();
            if( ctors.Length == 0 ) m.Error( $"No public constructor found for '{Type.FullName}'." );
            else if( ctors.Length > 1 ) m.Error( $"Multiple public constructors found for '{Type.FullName}'. Only one must exist." );
            else
            {
                success = Generalization?.EnsureCtorBinding( m, collector ) ?? true;
                var parameters = ctors[0].GetParameters();
                var mParameters = new List<CtorParameter>();
                foreach( var p in parameters )
                {
                    var param = CreateCtorParameter( m, collector, p );
                    success &= param.Success;
                    if( param.Class != null || param.Interface != null )
                    {
                        mParameters.Add( new CtorParameter( p, param.Class, param.Interface, param.IsEnumerable ) );
                    }
                    // We check here the Singleton to Scoped dependency error at the Type level.
                    // This must be done here since CtorParameters are not created for types that are external (those
                    // are considered as Scoped) or for ambient interfaces that have no implementation classes.
                    // If the parameter knwn to be singleton, we have nothing to do.
                    if( param.Lifetime == AmbientTypeKind.None || (param.Lifetime & AmbientTypeKind.IsScoped) != 0 )
                    {
                        // Note: if this DeclaredLifetime is AmbientScoped nothing is done here: as a
                        //       scoped service there is nothing to say about its constructor parameters' lifetime.
                        if( DeclaredLifetime == AmbientTypeKind.AmbientSingleton )
                        {
                            if( param.Lifetime == AmbientTypeKind.None )
                            {
                                m.Warn( $"Type '{p.Member.DeclaringType.Name}' is marked with {nameof( ISingletonAmbientService )}. Parameter '{p.Name}' of type '{p.ParameterType.Name}' that has no associated lifetime will be considered as a Singleton." );
                                if( collector.AmbientKindDetector.DefineAsSingletonReference( m, p.ParameterType ) == null )
                                {
                                    success = false;
                                }
                            }
                            else
                            {
                                MustBeScopedLifetime = true;
                                string paramReason;
                                if( param.Lifetime == AmbientTypeKind.AmbientScope )
                                {
                                    paramReason = $"is marked with {nameof( IScopedAmbientService )}";
                                }
                                else
                                {
                                    Debug.Assert( param.Lifetime == AmbientTypeKind.IsScoped );
                                    paramReason = $"is registered as an external scoped service";
                                }
                                m.Error( $"Lifetime error: Type '{p.Member.DeclaringType.Name}' is marked with {nameof( ISingletonAmbientService )}  but parameter '{p.Name}' of type '{p.ParameterType.Name}' {paramReason}." );
                                success = false;
                            }
                        }
                        else if( DeclaredLifetime == AmbientTypeKind.IsAmbientService )
                        {
                            if( (param.Lifetime & AmbientTypeKind.IsScoped) != 0 )
                            {
                                m.Info( $"{nameof( IAmbientService )} '{p.Member.DeclaringType.Name}' is Scoped because of parameter '{p.Name}' of type '{p.ParameterType.Name}'." );
                                MustBeScopedLifetime = true;
                            }
                            else
                            {
                                Debug.Assert( param.Lifetime == AmbientTypeKind.None );
                                if( _requiredParametersToBeSingletons == null ) _requiredParametersToBeSingletons = new List<ParameterInfo>();
                                _requiredParametersToBeSingletons.Add( p );
                            }
                        }
                    }
                    // Temporary: Enumeration is not implemented yet.
                    if( success && param.IsEnumerable )
                    {
                        m.Error( $"IEnumerable<T> or IReadOnlyList<T> where T is marked with IScopedAmbientService or ISingletonAmbientService is not supported yet: '{Type.FullName}' constructor cannot be handled." );
                        success = false;
                    }
                }
                ConstructorParameters = mParameters;
                ConstructorInfo = ctors[0];
            }
            _ctorBinding = success;
            return success;
        }

        readonly struct CtorParameterData
        {
            public readonly bool Success;
            public readonly AmbientServiceClassInfo Class;
            public readonly AmbientServiceInterfaceInfo Interface;
            public readonly bool IsEnumerable;
            public readonly AmbientTypeKind Lifetime;

            public CtorParameterData( bool success, AmbientServiceClassInfo c, AmbientServiceInterfaceInfo i, bool isEnumerable, AmbientTypeKind lt )
            {
                Success = success;
                Class = c;
                Interface = i;
                IsEnumerable = isEnumerable;
                Lifetime = lt;
            }
        }

        CtorParameterData CreateCtorParameter(
            IActivityMonitor m,
            AmbientTypeCollector collector,
            ParameterInfo p )
        {
            var tParam = p.ParameterType;
            bool isEnumerable = false;
            if( tParam.IsGenericType )
            {
                var tGen = tParam.GetGenericTypeDefinition();
                if( tGen == typeof( IEnumerable<> )
                    || tGen == typeof( IReadOnlyCollection<> )
                    || tGen == typeof( IReadOnlyList<> ) )
                {
                    isEnumerable = true;
                    tParam = tParam.GetGenericArguments()[0];
                }
                else 
                {
                    var genLifetime = collector.AmbientKindDetector.GetKind( m, tGen );
                    if( genLifetime != AmbientTypeKind.None )
                    {
                        return new CtorParameterData( true, null, null, false, genLifetime );
                    }
                }
            }
            // We only consider I(Scoped/Singleton)AmbientService marked type parameters.
            var lifetime = collector.AmbientKindDetector.GetKind( m, tParam );
            if( (lifetime&AmbientTypeKind.IsAmbientService) == 0 )
            {
                return new CtorParameterData( true, null, null, false, lifetime );
            }
            var conflictMsg = lifetime.GetAmbientKindCombinationError(); 
            if( conflictMsg != null )
            {
                m.Error( $"Type '{tParam.FullName}' for parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor: {conflictMsg}" );
                return new CtorParameterData( false, null, null, false, lifetime );
            }

            if( tParam.IsClass )
            {
                var sClass = collector.FindServiceClassInfo( tParam );
                if( sClass == null )
                {
                    m.Error( $"Unable to resolve '{tParam.FullName}' service type for parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor." );
                    return new CtorParameterData( false, null, null, isEnumerable, lifetime );
                }
                if( !sClass.IsIncluded )
                {
                    var reason = sClass.IsExcluded
                                    ? "excluded from registration"
                                    : "abstract (and can not be concretized)";
                    var prefix = $"Service type '{tParam.Name}' is {reason}. Parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor ";
                    if( !p.HasDefaultValue )
                    {
                        m.Error( prefix + "can not be resolved." );
                        return new CtorParameterData( false, null, null, isEnumerable, lifetime );
                    }
                    m.Info( prefix + "will use its default value." );
                    sClass = null;
                }
                else if( IsAssignableFrom( sClass ) )
                {
                    var prefix = $"Parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor ";
                    m.Error( prefix + "cannot be this class or one of its specializations." );
                    return new CtorParameterData( false, null, null, isEnumerable, lifetime );
                }
                else if( sClass.IsAssignableFrom( this ) )
                {
                    var prefix = $"Parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor ";
                    m.Error( prefix + "cannot be one of its base class." );
                    return new CtorParameterData( false, null, null, isEnumerable, lifetime );
                }
                return new CtorParameterData( true, sClass, null, isEnumerable, lifetime );
            }
            return new CtorParameterData( true, null, collector.FindServiceInterfaceInfo( tParam ), isEnumerable, lifetime );
        }

    }
}
