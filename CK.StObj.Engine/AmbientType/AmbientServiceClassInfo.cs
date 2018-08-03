using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CK.Core
{
    public class AmbientServiceClassInfo : AmbientTypeInfo
    {
        public class CtorParameter
        {
            public readonly ParameterInfo ParameterInfo;
            public readonly AmbientServiceClassInfo ServiceClass;
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
            AmbientServiceInterfaceInfo[] interfaces,
            Func<Type, AmbientServiceClassInfo> registerClass,
            Func<Type, AmbientServiceInterfaceInfo> registerInterface )
            : base( m, parent, t, serviceProvider )
        {
            Interfaces = interfaces;
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
                    AmbientServiceClassInfo sClass = null;
                    AmbientServiceInterfaceInfo sInterface = null;
                    if( typeof( IAmbientService ).IsAssignableFrom( p.ParameterType ) )
                    {
                        if( typeof( IAmbientService ) == p.ParameterType )
                        {
                            m.Error( $"Invalid use of {nameof( IAmbientService )} constructor parameter {p.Name} in {p.Member}." );
                            error = true;
                        }
                        else
                        {
                            if( p.ParameterType.IsClass )
                            {
                                sClass = registerClass( p.ParameterType );
                                if( sClass == null )
                                {
                                    m.Error( $"Unable to resolve {p.ParameterType.Name} service type for parameter {p.Name} in {p.Member}" );
                                    error = true;
                                }
                            }
                            else
                            {
                                sInterface = registerInterface( p.ParameterType );
                            }
                        }
                    }
                    mParameters[p.Position] = new CtorParameter( p, sClass, sInterface );
                }
                ConstructorParameters = mParameters;
                if( !error )
                {
                    Container = t.GetCustomAttribute<AmbientServiceAttribute>()?.Container;
                    if( Container == null )
                    {
                        m.Error( $"Missing {nameof( AmbientServiceAttribute )} on {t.FullName}. This attribute is required." );
                    }
                    else
                    {
                        ConstructorInfo = ctors[0];
                    }
                }
            }
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
