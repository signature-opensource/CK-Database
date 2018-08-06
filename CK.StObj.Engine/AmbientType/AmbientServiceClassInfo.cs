using System;
using System.Collections.Generic;
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
            Container = t.GetCustomAttribute<AmbientServiceAttribute>()?.Container;
            if( Container == null )
            {
                m.Error( $"Missing {nameof( AmbientServiceAttribute )} on {t.FullName}. This attribute is required." );
            }
            Interfaces = collector.RegisterServiceInterfaces( IsExcluded ? Array.Empty<Type>() : t.GetInterfaces() ).ToArray();

            if( IsExcluded ) return;
            var ctors = t.GetConstructors();
            if( ctors.Length == 0 ) m.Error( $"No public constructor found for {t.FullName}." );
            else if( ctors.Length > 1 ) m.Error( $"Multiple public constructors found for {t.FullName}. Only one must exist." );
            else
            {
                var parameters = ctors[0].GetParameters();
                var mParameters = new CtorParameter[parameters.Length];
                bool error = false;
                foreach( var p in parameters )
                {
                    var (success, sClass, sInterface) = HandleParameterTypes( m, collector, p );
                    error |= !success;
                    mParameters[p.Position] = new CtorParameter( p, sClass, sInterface );
                }
                if( !error )
                {
                    ConstructorParameters = mParameters;
                    ConstructorInfo = ctors[0];
                }
            }
        }

        static (bool,AmbientServiceClassInfo, AmbientServiceInterfaceInfo) HandleParameterTypes( IActivityMonitor m, AmbientTypeCollector collector, ParameterInfo p )
        {
            if( !typeof( IAmbientService ).IsAssignableFrom( p.ParameterType ) )
            {
                return (true, null, null);
            }
            if( typeof( IAmbientService ) == p.ParameterType )
            {
                m.Error( $"Invalid use of {nameof( IAmbientService )} constructor parameter {p.Name} in {p.Member}." );
                return (false,null, null);
            }
            if( p.ParameterType.IsClass )
            {
                var sClass = collector.RegisterCtorDepClass( p.ParameterType );
                if( sClass == null )
                {
                    m.Error( $"Unable to resolve {p.ParameterType.Name} service type for parameter {p.Name} in {p.Member}" );
                    return (false, null, null);
                }
                if( sClass.IsExcluded )
                {
                    if( p.HasDefaultValue )
                    {
                        m.Info( $"Service type {p.ParameterType.Name} is excluded from registration. Parameter {p.Name} in {p.Member} will use its default value." );
                    }
                    else
                    {
                        m.Error( $"Service type {p.ParameterType.Name} is excluded from registration. Parameter {p.Name} in {p.Member} can not be resolved." );
                        return (false, null, null);
                    }
                }
                return (true, sClass, null);
            }
            return (true, null, collector.RegisterServiceInterface( p.ParameterType ));
        }

        /// <summary>
        /// Gets the supported service interfaces.
        /// This is never null but may be empty if the service has no abstraction (there
        /// is no service interface, the implementation itself is marked with <see cref="IAmbientService"/>).
        /// </summary>
        public IReadOnlyList<AmbientServiceInterfaceInfo> Interfaces { get; }

        /// <summary>
        /// Gets the container type to which this service is associated.
        /// This can be null only if an error occured: Ambient services must always be
        /// associated to one and only one container.
        /// </summary>
        public Type Container { get; }

        /// <summary>
        /// Gets the constructor. This is null if any error occurred.
        /// </summary>
        public ConstructorInfo ConstructorInfo { get; }

        /// <summary>
        /// Gets the constructor parameters.
        /// </summary>
        public IReadOnlyList<CtorParameter> ConstructorParameters { get; }

    }
}
