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
            Interfaces = collector.RegisterServiceInterfaces( IsExcluded ? Array.Empty<Type>() : t.GetInterfaces() ).ToArray();

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
        /// Gets the supported service interfaces.
        /// This is never null but may be empty if the service is excluded or has no abstraction (there
        /// is no service interface, the implementation itself is marked with <see cref="IAmbientService"/>).
        /// </summary>
        public IReadOnlyList<AmbientServiceInterfaceInfo> Interfaces { get; }

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

        internal bool EnsureTypeImplementation( IActivityMonitor monitor, IDynamicAssembly assembly )
        {
            Debug.Assert( !IsExcluded && !IsSpecialized && ImplementableTypeInfo == null );
            if( !Type.IsAbstract ) return true;
            ImplementableTypeInfo = CreateAbstractTypeImplementation( monitor, assembly );
            return ImplementableTypeInfo != null;
        }

        internal MutableItem BindToContainerItem( IActivityMonitor monitor, StObjObjectEngineMap engineMap )
        {
            Debug.Assert( !IsExcluded && ContainerType != null );
            return ContainerItem = engineMap.ToHighestImpl( ContainerType );
        }

        internal bool BindToSingleCtorParameters( IActivityMonitor m, AmbientTypeCollector collector )
        {
            bool succces = false;
            var ctors = Type.GetConstructors();
            if( ctors.Length == 0 ) m.Error( $"No public constructor found for {Type.FullName}." );
            else if( ctors.Length > 1 ) m.Error( $"Multiple public constructors found for {Type.FullName}. Only one must exist." );
            else
            {
                succces = true;
                var parameters = ctors[0].GetParameters();
                var mParameters = new CtorParameter[parameters.Length];
                foreach( var p in parameters )
                {
                    var (success, sClass, sInterface) = RegisterParameterTypes( m, collector, p );
                    succces &= success;
                    mParameters[p.Position] = new CtorParameter( p, sClass, sInterface );
                }
                ConstructorParameters = mParameters;
                ConstructorInfo = ctors[0];
            }
            return succces;
        }
        static (bool, AmbientServiceClassInfo, AmbientServiceInterfaceInfo) RegisterParameterTypes( IActivityMonitor m, AmbientTypeCollector collector, ParameterInfo p )
        {
            // We only consider IAmbientService interface or type parameters.
            if( !typeof( IAmbientService ).IsAssignableFrom( p.ParameterType ) )
            {
                return (true, null, null);
            }
            // Edge case: using IAmbientService is an error.
            if( typeof( IAmbientService ) == p.ParameterType )
            {
                m.Error( $"Invalid use of {nameof( IAmbientService )} constructor parameter {p.Name} in {p.Member.DeclaringType.FullName} constructor." );
                return (false, null, null);
            }
            if( p.ParameterType.IsClass )
            {
                var sClass = collector.FindServiceClassInfo( p.ParameterType );
                if( sClass == null )
                {
                    m.Error( $"Unable to resolve {p.ParameterType.Name} service type for parameter {p.Name} in {p.Member.DeclaringType.FullName} constructor." );
                    return (false, null, null);
                }
                if( sClass.IsExcluded )
                {
                    if( !p.HasDefaultValue )
                    {
                        m.Error( $"Service type {p.ParameterType.Name} is excluded from registration. Parameter {p.Name} in {p.Member.DeclaringType.FullName} constructor can not be resolved." );
                        return (false, null, null);
                    }
                    m.Info( $"Service type {p.ParameterType.Name} is excluded from registration. Parameter {p.Name} in {p.Member.DeclaringType.FullName} constructor will use its default value." );
                    sClass = null;
                }
                return (true, sClass, null);
            }
            return (true, null, collector.FindServiceInterfaceInfo( p.ParameterType ));
        }

    }
}
