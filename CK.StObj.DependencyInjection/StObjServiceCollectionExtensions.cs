using CK.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds extension methods on <see cref="IServiceCollection"/>.
    /// </summary>
    public static class StObjServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the <see cref="IStObjMap.StObjs"/> and the <see cref="IStObjMap"/> itself as Singletons
        /// and <see cref="IStObjMap.Services"/> as Scoped or Singleton services.
        /// <para>
        /// Assembly load conflicts may occur here. In such case, you should use the CK.WeakAssemblyNameResolver package
        /// and wrap the call this way:
        /// <code>
        /// using( CK.Core.WeakAssemblyNameResolver.TemporaryInstall() )
        /// {
        ///     services.AddStObjMap( stobjAssembly );
        /// }
        /// </code>
        /// Note that there SHOULD NOT be any conflicts. This workaround may be necessary but hides a conflict of version dependencies
        /// that may cause runtime errors.
        /// </para>
        /// </summary>
        /// <param name="services">This services.</param>
        /// <param name="stobjAssembly">The assembly.</param>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddStObjMap( this IServiceCollection services, Assembly stobjAssembly )
        {
            if( stobjAssembly == null ) throw new ArgumentNullException( nameof( stobjAssembly ) );
            var map = StObjContextRoot.Load( stobjAssembly );
            if( map == null )
                throw new ArgumentException( $"The assembly {stobjAssembly.FullName} was not found or is not a valid StObj map assembly" );
            return AddStObjMap( services, map );
        }

        /// <summary>
        /// Registers the <see cref="IStObjMap.StObjs"/> and the <see cref="IStObjMap"/> itself as Singletons
        /// and <see cref="IStObjMap.Services"/> as Scoped or Singleton services.
        /// <para>
        /// Assembly load conflicts may occur here. In such case, you should use the CK.WeakAssemblyNameResolver package
        /// and wrap the call this way:
        /// <code>
        /// using( CK.Core.WeakAssemblyNameResolver.TemporaryInstall() )
        /// {
        ///     services.AddStObjMap( "CK.StObj.AutoAssembly" );
        /// }
        /// </code>
        /// Note that there SHOULD NOT be any conflicts. This workaround may be necessary but hides a conflict of version dependencies
        /// that may cause runtime errors.
        /// </para>
        /// </summary>
        /// <param name="services">This services.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <remarks>
        /// On NetCore runtime, Assembly.LoadFrom is used to resolves the assembly from its full path.
        /// </remarks>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddStObjMap( this IServiceCollection services, string assemblyName )
        {
#if NET461
            return AddStObjMap( services, new AssemblyName( assemblyName ) );
#else
            string path = System.IO.Path.Combine( AppDomain.CurrentDomain.BaseDirectory, assemblyName + ".dll" );
            return AddStObjMap( services, Assembly.LoadFrom( path ) );
#endif
        }

        /// <summary>
        /// Registers the <see cref="IStObjMap.StObjs"/> and the <see cref="IStObjMap"/> itself as Singletons
        /// and <see cref="IStObjMap.Services"/> as Scoped or Singleton services.
        /// <para>
        /// Assembly load conflicts may occur here. In such case, you should use the CK.WeakAssemblyNameResolver package
        /// and wrap the call this way:
        /// <code>
        /// using( CK.Core.WeakAssemblyNameResolver.TemporaryInstall() )
        /// {
        ///     services.AddStObjMap( assemblyName );
        /// }
        /// </code>
        /// Note that there SHOULD NOT be any conflicts. This workaround may be necessary but hides a conflict of version dependencies
        /// that may cause runtime errors.
        /// </para>
        /// </summary>
        /// <param name="services">This services.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddStObjMap( this IServiceCollection services, AssemblyName assemblyName )
        {
            return services.AddStObjMap( Assembly.Load( assemblyName ) );
        }

        /// <summary>
        /// Registers the <see cref="IStObjMap.StObjs"/> and the <see cref="IStObjMap"/> itself as Singletons
        /// and <see cref="IStObjMap.Services"/> as Scoped or Singleton services.
        /// </summary>
        /// <param name="services">This services.</param>
        /// <param name="map">StObj map to register.</param>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddStObjMap( this IServiceCollection services, IStObjMap map )
        {
            if( map == null ) throw new ArgumentNullException( nameof( map ) );
            services.AddSingleton( map );
            foreach( var kv in map.StObjs.Mappings )
            {
                services.AddSingleton( kv.Key, kv.Value );
            }
            // Service direct type mapping.
            services.AddServiceSimpleMappings( map.Services.SimpleMappings );

            // Manual type: Use the automatically generated code.
            services.AddServiceManualMappings( map.Services.ManualMappings );

            return services;
        }

        public static IServiceCollection AddServiceSimpleMappings( this IServiceCollection services, IReadOnlyDictionary<Type, IStObjServiceClassDescriptor> mappings )
        {
            // Service direct type mapping.
            foreach( var kv in mappings )
            {
                if( kv.Value.IsScoped )
                {
                    services.TryAddScoped( kv.Value.ClassType );
                    if( kv.Key != kv.Value.ClassType ) services.AddScoped( kv.Key, sp => sp.GetRequiredService( kv.Value.ClassType ) );
                }
                else
                {
                    services.TryAddSingleton( kv.Value.ClassType );
                    if( kv.Key != kv.Value.ClassType ) services.AddSingleton( kv.Key, sp => sp.GetRequiredService( kv.Value.ClassType ) );
                }
            }

            return services;
        }

        public static IServiceCollection AddServiceManualMappings( this IServiceCollection services, IReadOnlyDictionary<Type, IStObjServiceClassFactory> mappings )
        {
            foreach( var kv in mappings )
            {
                if( kv.Value.IsScoped )
                {
                    services.AddScoped( kv.Key, p => kv.Value.CreateInstance( p ) );
                }
                else
                {
                    services.AddSingleton( kv.Key, p => kv.Value.CreateInstance( p ) );
                }
            }
            return services;
        }
    }
}
