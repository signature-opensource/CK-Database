using CK.Core;
using System;
using CK.SqlServer.Setup;
using System.Reflection;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds AddCKDatabase extension methods on <see cref="IServiceCollection"/>.
    /// </summary>
    public static class CKDBServiceCollectionExtensions
    {
        /// <summary>
        /// Calls <see cref="StObjServiceCollectionExtensions.AddStObjMap(IServiceCollection, IActivityMonitor, Assembly, SimpleServiceContainer)">StObjMap registration</see>
        /// and optionnally configures the <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// <para>
        /// Assembly load conflicts may occur here. In such case, you should use the CK.WeakAssemblyNameResolver package
        /// and wrap the call this way:
        /// <code>
        /// using( CK.Core.WeakAssemblyNameResolver.TemporaryInstall() )
        /// {
        ///     services.AddCKDatabase( stobjAssembly );
        /// }
        /// </code>
        /// Note that there SHOULD NOT be any conflicts. This workaround may be necessary but hides a conflict of version dependencies
        /// that may cause runtime errors.
        /// </para>
        /// </summary>
        /// <param name="services">This services.</param>
        /// <param name="monitor">The monitor to use. Must not be null.</param>
        /// <param name="stobjAssembly">The assembly.</param>
        /// <param name="defaultConnectionString">
        /// Optional connection string that will override <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// </param>
        /// <param name="startupServices">
        /// Optional simple container that may provide startup services. This is not used to build IAmbientObject
        /// (they must be independent of any "dynamic" services), however registered services become available to
        /// any <see cref="StObjContextRoot.ConfigureServicesMethodName"/> methods by parameter injection.
        /// </param>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddCKDatabase( this IServiceCollection services, IActivityMonitor monitor, Assembly stobjAssembly, string defaultConnectionString = null, SimpleServiceContainer startupServices = null )
        {
            return CKDatabasify( services.AddStObjMap( monitor, stobjAssembly, startupServices ), defaultConnectionString );
        }

        /// <summary>
        /// Calls <see cref="StObjServiceCollectionExtensions.AddStObjMap(IServiceCollection, IActivityMonitor, string, SimpleServiceContainer)">StObjMap registration</see>
        /// and optionnally configures the <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// <para>
        /// Assembly load conflicts may occur here. In such case, you should use the CK.WeakAssemblyNameResolver package
        /// and wrap the call this way:
        /// <code>
        /// using( CK.Core.WeakAssemblyNameResolver.TemporaryInstall() )
        /// {
        ///     services.AddCKDatabase( "CK.StObj.AutoAssembly" );
        /// }
        /// </code>
        /// Note that there SHOULD NOT be any conflicts. This workaround may be necessary but hides a conflict of version dependencies
        /// that may cause runtime errors.
        /// </para>
        /// </summary>
        /// <param name="services">This services.</param>
        /// <param name="monitor">The monitor to use. Must not be null.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="defaultConnectionString">
        /// Optional connection string that will override <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// </param>
        /// <param name="startupServices">
        /// Optional simple container that may provide startup services. This is not used to build IAmbientObject
        /// (they must be independent of any "dynamic" services), however registered services become available to
        /// any <see cref="StObjContextRoot.ConfigureServicesMethodName"/> methods by parameter injection.
        /// </param>
        /// <remarks>
        /// On NetCore runtime, Assembly.LoadFrom is used to resolves the assembly from its full path.
        /// </remarks>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddCKDatabase( this IServiceCollection services, IActivityMonitor monitor, string assemblyName, string defaultConnectionString = null, SimpleServiceContainer startupServices = null )
        {
            return CKDatabasify( services.AddStObjMap( monitor, assemblyName, startupServices ), defaultConnectionString );
        }

        /// <summary>
        /// Calls <see cref="StObjServiceCollectionExtensions.AddStObjMap(IServiceCollection, IActivityMonitor, AssemblyName, SimpleServiceContainer)">StObjMap registration</see>
        /// and optionnally configures the <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// <para>
        /// Assembly load conflicts may occur here. In such case, you should use the CK.WeakAssemblyNameResolver package
        /// and wrap the call this way:
        /// <code>
        /// using( CK.Core.WeakAssemblyNameResolver.TemporaryInstall() )
        /// {
        ///     services.AddCKDatabase( "CK.StObj.AutoAssembly" );
        /// }
        /// </code>
        /// Note that there SHOULD NOT be any conflicts. This workaround may be necessary but hides a conflict of version dependencies
        /// that may cause runtime errors.
        /// </para>
        /// </summary>
        /// <param name="services">This services.</param>
        /// <param name="monitor">The monitor to use. Must not be null.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="defaultConnectionString">
        /// Optional connection string that will override <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// </param>
        /// <param name="startupServices">
        /// Optional simple container that may provide startup services. This is not used to build IAmbientObject
        /// (they must be independent of any "dynamic" services), however registered services become available to
        /// any <see cref="StObjContextRoot.ConfigureServicesMethodName"/> methods by parameter injection.
        /// </param>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddCKDatabase( this IServiceCollection services, IActivityMonitor monitor, AssemblyName assemblyName, string defaultConnectionString = null, SimpleServiceContainer startupServices = null )
        {
            return CKDatabasify( services.AddStObjMap( monitor, assemblyName, startupServices ), defaultConnectionString );
        }


        /// <summary>
        /// Calls <see cref="StObjServiceCollectionExtensions.AddStObjMap(IServiceCollection, IActivityMonitor, IStObjMap, SimpleServiceContainer)">StObjMap registration</see>
        /// and optionnally configures the <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// </summary>
        /// <param name="services">This services.</param>
        /// <param name="monitor">The monitor to use. Must not be null.</param>
        /// <param name="map">StObj map to register. Must not be null.</param>
        /// <param name="defaultConnectionString">
        /// Optional connection string that will override <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// </param>
        /// <param name="startupServices">
        /// Optional simple container that may provide startup services. This is not used to build IAmbientObject
        /// (they must be independent of any "dynamic" services), however registered services become available to
        /// any <see cref="StObjContextRoot.ConfigureServicesMethodName"/> methods by parameter injection.
        /// </param>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddCKDatabase( this IServiceCollection services, IActivityMonitor monitor, IStObjMap map, string defaultConnectionString = null, SimpleServiceContainer startupServices = null )
        {
            return CKDatabasify( services.AddStObjMap( monitor, map, startupServices ), defaultConnectionString );
        }

        static IServiceCollection CKDatabasify( IServiceCollection services, string defaultConnectionString )
        {
            if( !String.IsNullOrEmpty( defaultConnectionString ) )
            {
                var map = (IStObjMap)services.Last( d => d.ServiceType == typeof( IStObjMap ) ).ImplementationInstance;
                map.StObjs.Obtain<SqlDefaultDatabase>().ConnectionString = defaultConnectionString;
            }
            return services;
        }

    }
}
