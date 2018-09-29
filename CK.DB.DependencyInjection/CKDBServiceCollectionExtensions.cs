using CK.Core;
using System;
using CK.SqlServer.Setup;
using System.Reflection;
using System.Linq;
using CK.SqlServer;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds AddCKDatabase extension methods on <see cref="IServiceCollection"/>.
    /// </summary>
    public static class CKDBServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the <see cref="IStObjMap.StObjs"/> and the <see cref="IStObjMap"/> itself as Singletons
        /// and <see cref="IStObjMap.Services"/> as Scoped services from a <see cref="IStObjMap"/>.
        /// Registers as a Scoped service <see cref="ISqlCallContext"/> mapped to <see cref="SqlStandardCallContext"/>
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
        /// <param name="stobjAssembly">The assembly.</param>
        /// <param name="defaultConnectionString">
        /// Optional connection string that will override <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// </param>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddCKDatabase( this IServiceCollection services, Assembly stobjAssembly, string defaultConnectionString = null )
        {
            return CKDatabasify( services.AddStObjMap( stobjAssembly ), defaultConnectionString );
        }

        /// <summary>
        /// Registers the <see cref="IStObjMap.StObjs"/> and the <see cref="IStObjMap"/> itself as Singletons
        /// and <see cref="IStObjMap.Services"/> as Scoped services from a <see cref="IStObjMap"/>.
        /// Registers as a Scoped service <see cref="ISqlCallContext"/> mapped to <see cref="SqlStandardCallContext"/>
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
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="defaultConnectionString">
        /// Optional connection string that will override <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// </param>
        /// <remarks>
        /// On NetCore runtime, Assembly.LoadFrom is used to resolves the assembly from its full path.
        /// </remarks>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddCKDatabase( this IServiceCollection services, string assemblyName, string defaultConnectionString = null )
        {
            return CKDatabasify( services.AddStObjMap( assemblyName ), defaultConnectionString );
        }

        /// <summary>
        /// Registers the <see cref="IStObjMap.StObjs"/> and the <see cref="IStObjMap"/> itself as Singletons
        /// and <see cref="IStObjMap.Services"/> as Scoped services from a <see cref="IStObjMap"/>.
        /// Registers as a Scoped service <see cref="ISqlCallContext"/> mapped to <see cref="SqlStandardCallContext"/>
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
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="defaultConnectionString">
        /// Optional connection string that will override <see cref="SqlDefaultDatabase"/> <see cref="SqlDatabase.ConnectionString">ConnectionString</see>.
        /// </param>
        /// <returns>This services collection.</returns>
        public static IServiceCollection AddCKDatabase( this IServiceCollection services, AssemblyName assemblyName, string defaultConnectionString = null )
        {
            return CKDatabasify( services.AddStObjMap( assemblyName ), defaultConnectionString );
        }

        static IServiceCollection CKDatabasify( IServiceCollection services, string defaultConnectionString )
        {
            if( !String.IsNullOrEmpty( defaultConnectionString ) )
            {
                var map = (IStObjMap)services.First( d => d.ServiceType == typeof( IStObjMap ) ).ImplementationInstance;
                map.StObjs.Obtain<SqlDefaultDatabase>().ConnectionString = defaultConnectionString;
            }
            services.AddScoped<ISqlCallContext, SqlStandardCallContext>();
            return services;
        }

    }
}
