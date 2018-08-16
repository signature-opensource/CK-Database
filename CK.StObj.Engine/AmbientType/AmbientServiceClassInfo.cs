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
    /// 
    /// </summary>
    public class AmbientServiceClassInfo : AmbientTypeInfo
    {
        HashSet<AmbientServiceClassInfo> _ctorParmetersClosure;
        bool? _ctorBinding;

        /// <summary>
        /// Constructor parameter info: some of them are <see cref="AmbientServiceClassInfo"/>
        /// or <see cref="AmbientServiceInterfaceInfo"/>.
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

            internal CtorParameter(
                ParameterInfo p,
                AmbientServiceClassInfo cS,
                AmbientServiceInterfaceInfo iS )
            {
                ParameterInfo = p;
                ServiceClass = cS;
                ServiceInterface = iS;
            }

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
            bool isExcluded )
            : base( m, parent, t, serviceProvider, isExcluded )
        {
            Debug.Assert( Generalization == parent );
            if( parent != null ) SpecializationDepth = parent.SpecializationDepth + 1;
            if( IsExcluded ) return;

            var aC = t.GetCustomAttribute<AmbientServiceAttribute>();
            if( aC == null )
            {
                m.Warn( $"Missing {nameof( AmbientServiceAttribute )} on '{t.FullName}'." );
            }
            else
            {
                ContainerType = aC.Container;
                if( ContainerType == null )
                {
                    m.Info( $"{nameof( AmbientServiceAttribute )} on '{t.FullName}' indicates no container." );
                }
            }
        }

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
        /// This is not null only if <see cref="IsIncluded"/> is true (ie. this class is not excluded and is on a concrete path)
        /// and may be empty if there is no service interface (the implementation itself is marked
        /// with <see cref="IAmbientService"/>).
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
        /// Gets the constructor parameters.
        /// </summary>
        public IReadOnlyList<CtorParameter> ConstructorParameters { get; private set; }

        /// <summary>
        /// Gets the <see cref="ImplementableTypeInfo"/> if this <see cref="AmbientTypeInfo.Type"/> is abstract.
        /// </summary>
        public ImplementableTypeInfo ImplementableTypeInfo { get; private set; }

        /// <summary>
        /// Gets the specialization depth from the first top AmbientServiceClassInfo.
        /// This is not the same as <see cref="StObjTypeInfo.SpecializationDepth"/> that
        /// is relative to <see cref="Object"/> type.
        /// </summary>
        public int SpecializationDepth { get; }

        /// <summary>
        /// Gets whether this class is on a concrete path: it is not excluded and is not abstract
        /// or has at least one concrete specialization.
        /// Only included classes eventually participate to the setup process.
        /// </summary>
        public bool IsIncluded => Interfaces != null;

        internal void FillFinalClassMappings( Dictionary<Type,AmbientServiceClassInfo> mappings, List<AmbientServiceClassInfo> subGraphCollector )
        {
            Debug.Assert( IsIncluded );
            if( MostSpecialized == null ) MostSpecialized = this;
            mappings.Add( Type, MostSpecialized );
            foreach( var s in Specializations )
            {
                if( s.MostSpecialized != MostSpecialized ) subGraphCollector.Add( s );
                s.FillFinalClassMappings( mappings, subGraphCollector );
            }
        }

        /// <summary>
        /// This mimics the <see cref="StObjTypeInfo.CreateMutableItemsPath"/> method
        /// to reproduce the exact same Type handling between Services and StObj (ignoring agstract tails
        /// for instance).
        /// This is simpler since there is no split in type info (no MutableItem layer).
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
        /// instance potentially up to the leaf (and handles
        /// container binding at the same time).
        /// At least one assignment (the one of this instance)
        /// is necessarily done.
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

        internal bool InitializeMostSpecialized( IActivityMonitor monitor, AmbientTypeCollector collector, StObjObjectEngineMap engineMap )
        {
            Debug.Assert( IsIncluded );
            bool success = EnsureCtorBinding( monitor, collector );
            var g = this;
            do
            {
                g.MostSpecialized = this;
                if( g.ContainerType != null )
                {
                    if( (g.ContainerItem = engineMap.ToHighestImpl( g.ContainerType )) == null )
                    {
                        monitor.Error( $"Unable to resolve container '{g.ContainerType.FullName}' for service '{g.Type.FullName}' to a StObj." );
                        success = false;
                    }
                }
            }
            while( (g = g.Generalization) != null );
            return success;
        }

        internal HashSet<AmbientServiceClassInfo> ComputedCtorParametersClassClosure
        {
            get
            {
                Debug.Assert( _ctorParmetersClosure != null && _ctorBinding == true );
                return _ctorParmetersClosure;
            }
        }

        internal HashSet<AmbientServiceClassInfo> GetCtorParametersClassClosure( IActivityMonitor m, AmbientTypeCollector collector, ref bool initializationError )
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
                    foreach( var p in ConstructorParameters )
                    {
                        if( p.ServiceClass != null )
                        {
                            var c = p.ServiceClass;
                            do { _ctorParmetersClosure.Add( c ); } while( (c = c.Generalization) != null );
                            var cParams = p.ServiceClass.GetCtorParametersClassClosure( m, collector, ref initializationError );
                            _ctorParmetersClosure.UnionWith( cParams );
                        }
                    }
                }
            }
            return _ctorParmetersClosure;
        }

        internal bool EnsureCtorBinding( IActivityMonitor m, AmbientTypeCollector collector )
        {
            Debug.Assert( IsIncluded );
            if( _ctorBinding.HasValue ) return _ctorBinding.Value;
            bool succces = false;
            var ctors = Type.GetConstructors();
            if( ctors.Length == 0 ) m.Error( $"No public constructor found for '{Type.FullName}'." );
            else if( ctors.Length > 1 ) m.Error( $"Multiple public constructors found for '{Type.FullName}'. Only one must exist." );
            else
            {
                succces = Generalization?.EnsureCtorBinding( m, collector ) ?? true;
                var parameters = ctors[0].GetParameters();
                var mParameters = new CtorParameter[parameters.Length];
                foreach( var p in parameters )
                {
                    var (success, sClass, sInterface) = RegisterParameterTypes( m, collector, p );
                    succces &= success;
                    var param = new CtorParameter( p, sClass, sInterface );
                    mParameters[p.Position] = param;
                }
                ConstructorParameters = mParameters;
                ConstructorInfo = ctors[0];
            }
            _ctorBinding = succces;
            return succces;
        }

        (bool, AmbientServiceClassInfo, AmbientServiceInterfaceInfo) RegisterParameterTypes( IActivityMonitor m, AmbientTypeCollector collector, ParameterInfo p )
        {
            // We only consider IAmbientService interface or type parameters.
            if( !typeof( IAmbientService ).IsAssignableFrom( p.ParameterType ) )
            {
                return (true, null, null);
            }
            // Edge case: using IAmbientService is an error.
            if( typeof( IAmbientService ) == p.ParameterType )
            {
                m.Error( $"Invalid use of {nameof( IAmbientService )} constructor parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor." );
                return (false, null, null);
            }
            if( p.ParameterType.IsClass )
            {
                var sClass = collector.FindServiceClassInfo( p.ParameterType );
                if( sClass == null )
                {
                    m.Error( $"Unable to resolve '{p.ParameterType.FullName}' service type for parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor." );
                    return (false, null, null);
                }
                if( !sClass.IsIncluded )
                {
                    var reason = sClass.IsExcluded
                                    ? "excluded from registration"
                                    : "abstract (and can not be concretized)";
                    var prefix = $"Service type '{p.ParameterType.Name}' is {reason}. Parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor ";
                    if( !p.HasDefaultValue )
                    {
                        m.Error( prefix + "can not be resolved." );
                        return (false, null, null);
                    }
                    m.Info( prefix + "will use its default value." );
                    sClass = null;
                }
                else if( IsAssignableFrom( sClass ) )
                {
                    var prefix = $"Parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor ";
                    m.Error( prefix + "cannot be this class or one of its specializations." );
                    return (false, null, null);
                }
                else if( sClass.IsAssignableFrom( this ) )
                {
                    var prefix = $"Parameter '{p.Name}' in '{p.Member.DeclaringType.FullName}' constructor ";
                    m.Error( prefix + "cannot be one of its base class." );
                    return (false, null, null);
                }
                return (true, sClass, null);
            }
            return (true, null, collector.FindServiceInterfaceInfo( p.ParameterType ) );
        }

    }
}
