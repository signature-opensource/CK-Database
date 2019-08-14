using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CK.Core
{
    public abstract partial class StObjContextRoot
    {
        /// <summary>
        /// Small helper that captures the minimal required context to configure a <see cref="IServiceCollection"/>.
        /// See <see cref="StObjServiceCollectionExtensions.AddStObjMap(IServiceCollection, IActivityMonitor, IStObjMap, SimpleServiceContainer)"/>.
        /// </summary>
        public readonly struct ServiceRegister
        {
            enum RegType : byte
            {
                AmbienObject,
                Internal,
                PreviouslyRegistered,
            }
            readonly Dictionary<Type, RegType> _registered;

            /// <summary>
            /// Initializes a new <see cref="ServiceRegister"/>.
            /// </summary>
            /// <param name="monitor">The monitor to use. Must not be null.</param>
            /// <param name="services">The service collection to configure.</param>
            /// <param name="startupServices">
            /// Optional simple container that may provide startup services. This is not used to build IAmbientObject
            /// (they must be independent of any "dynamic" services), however registered services become available to
            /// any <see cref="StObjContextRoot.ConfigureServicesMethodName"/> methods by parameter injection.
            /// </param>
            public ServiceRegister( IActivityMonitor monitor, IServiceCollection services, SimpleServiceContainer startupServices = null )
            {
                Monitor = monitor ?? throw new ArgumentNullException( nameof( monitor ) );
                Services = services ?? throw new ArgumentNullException( nameof( services ) );
                StartupServices = startupServices ?? new SimpleServiceContainer();
                _registered = new Dictionary<Type, RegType>();
                foreach( var r in services ) _registered[r.ServiceType] = RegType.PreviouslyRegistered;
                AllowOverride = false;
            }

            /// <summary>
            /// Gets the monitor to use.
            /// </summary>
            public IActivityMonitor Monitor { get; }

            /// <summary>
            /// Gets the target service collection.
            /// </summary>
            public IServiceCollection Services { get; }

            /// <summary>
            /// Gets the startup services container.
            /// </summary>
            public SimpleServiceContainer StartupServices { get; }

            /// <summary>
            /// Gets whether registration should override any existing registration.
            /// Defaults to false: services must not already exist.
            /// </summary>
            public bool AllowOverride { get; }

            /// <summary>
            /// Registers the map, the Ambient objects, singleton services and scoped services.
            /// </summary>
            /// <param name="map">The map to register. Must not be null.</param>
            /// <returns>
            /// True on success, false if any <see cref="LogLevel.Fatal"/> or <see cref="LogLevel.Error"/> has been logged or if an exception has been thrown.
            /// </returns>
            public bool AddStObjMap( IStObjMap map )
            {
                bool result = true;
                using( Monitor.OnError( () => result = false ) )
                using( Monitor.OpenInfo( "Configuring Service collection from StObjMap." ) )
                {
                    try
                    {
                        if( map == null ) throw new ArgumentNullException( nameof( map ) );
                        DoRegisterSingleton( typeof( IStObjMap ), map, RegType.AmbienObject );
                        foreach( var kv in map.StObjs.Mappings )
                        {
                            DoRegisterSingleton( kv.Key, kv.Value, RegType.AmbienObject );
                        }
                        map.StObjs.ConfigureServices( this );
                        foreach( var kv in map.Services.SimpleMappings )
                        {
                            Register( kv.Key, kv.Value.ClassType, kv.Value.IsScoped );
                        }
                        foreach( var kv in map.Services.ManualMappings )
                        {
                            Register( kv.Key, kv.Value.CreateInstance, kv.Value.IsScoped );
                        }
                    }
                    catch( Exception ex )
                    {
                        Monitor.Error( "While registering StObjMap.", ex );
                    }
                }
                return result;
            }

            /// <summary>
            /// Registers an existing implementation as a singleton.
            /// </summary>
            /// <param name="serviceType">Service type.</param>
            /// <param name="implementation">Resolved singleton instance.</param>
            public void RegisterSingleton( Type serviceType, object implementation )
            {
                DoRegisterSingleton( serviceType, implementation, RegType.Internal );
            }

            void DoRegisterSingleton( Type serviceType, object implementation, RegType r )
            {
                if( !_registered.TryGetValue( serviceType, out var reg ) )
                {
                    Monitor.Trace( $"Registering service mapping from '{serviceType.Name}' to provided singleton instance." );
                    Services.Add( new ServiceDescriptor( serviceType, implementation ) );
                    _registered.Add( serviceType, r );
                }
                else if( reg == RegType.PreviouslyRegistered )
                {
                    Monitor.Warn( $"Service mapping '{serviceType.Name}' is already registered in ServiceCollection. Skipped singleton instance registration." );
                }
                else
                {
                    Monitor.Error( $"Duplicate '{serviceType.Name}' registration in ServiceCollection (singleton instance registration). ServiceRegister checks that registration occur at most once." );
                }
            }

            /// <summary>
            /// Registers a type mapping, ensuring that the <paramref name="implementation"/> itself is
            /// registered.
            /// </summary>
            /// <param name="serviceType">Service type.</param>
            /// <param name="implementation">Implementation type.</param>
            /// <param name="isScoped">True for scope, false for singletons.</param>
            public void Register( Type serviceType, Type implementation, bool isScoped )
            {
                ServiceLifetime lt = isScoped ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
                if( !_registered.TryGetValue( serviceType, out var reg ) )
                {
                    // When there is a mapping (the serviceType is not the target implementation), we must register
                    // a factory here: by registering the implementation, a new instance is created but we want the
                    // same instance!
                    if( serviceType != implementation )
                    {
                        Monitor.Trace( $"Registering service mapping from '{serviceType.Name}' to type '{implementation}' as {lt}." );
                        Services.Add( new ServiceDescriptor( serviceType, sp => sp.GetRequiredService( implementation ), lt ) );
                        _registered.Add( serviceType, RegType.Internal );
                    }
                    if( !_registered.TryGetValue( implementation, out reg ) )
                    {
                        Monitor.Trace( $"Registering service type '{implementation}' as {lt}." );
                        Services.Add( new ServiceDescriptor( implementation, implementation, lt ) );
                        _registered.Add( implementation, RegType.Internal );
                    }
                    else if( reg == RegType.PreviouslyRegistered )
                    {
                        Monitor.Warn( $"Service type '{implementation}' is already registered in ServiceCollection. {lt} registration skipped." );
                    }
                    else if( reg != RegType.AmbienObject )
                    {
                        Monitor.Error( $"Duplicate '{implementation}' type registration in ServiceRegister. ServiceRegister checks that registration occur at most once." );
                    }
                }
                else if( reg == RegType.PreviouslyRegistered )
                {
                    Monitor.Warn( $"Service mapping '{serviceType.Name}' is already registered in ServiceCollection. {lt} registration skipped." );
                }
                else if( reg != RegType.AmbienObject )
                {
                    Monitor.Error( $"Duplicate '{serviceType.Name}' registration in ServiceRegister (mapped to {implementation}). ServiceRegister checks that registration occur at most once." );
                }
            }

            /// <summary>
            /// Registers a factory method.
            /// </summary>
            /// <param name="serviceType">Service type.</param>
            /// <param name="factory">Instance factory.</param>
            /// <param name="isScoped">True for scope, false for singletons.</param>
            public void Register( Type serviceType, Func<IServiceProvider, object> factory, bool isScoped )
            {
                ServiceLifetime lt = isScoped ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
                // When there is a mapping (the serviceType is not the target implementation), we must register
                // a factory here: by registering the implementation, a new instance is created but we want the
                // same instance!
                if( !_registered.TryGetValue( serviceType, out var reg ) )
                {
                    Monitor.Trace( $"Registering factory method for service '{serviceType.Name}' as {lt}." );
                    Services.Add( new ServiceDescriptor( serviceType, factory, lt ) );
                    _registered.Add( serviceType, RegType.Internal );
                }
                else if( reg == RegType.PreviouslyRegistered )
                {
                    Monitor.Warn( $"Service '{serviceType.Name}' is already registered in ServiceRegister. Skipping {lt} factory method registration." );
                }
                else
                {
                    Monitor.Error( $"Unable to register mapping of '{serviceType.Name}' to a factory method since the type has already been mapped. ServiceRegister checks that registration occur at most once." );
                }
            }
        }
    }
}
