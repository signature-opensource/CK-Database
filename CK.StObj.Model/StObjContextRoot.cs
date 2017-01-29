#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObjContextRoot.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Reflection.Emit;
using System.Threading;
using System.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Abstract root object that is a <see cref="IStObjMap"/> and is able to build and load concrete maps thanks
    /// to static methods.
    /// </summary>
    public abstract partial class StObjContextRoot : IStObjMap
    {
        /// <summary>
        /// Holds the name of the root class.
        /// </summary>
        public static readonly string RootContextTypeName = "CK.StObj.GeneratedRootContext";

        static readonly HashSet<Assembly> _alreadyLoaded = new HashSet<Assembly>();

        /// <summary>
        /// Loads a previously generated assembly by its assembly name.
        /// </summary>
        /// <param name="assemblyName">Assembly name that will be loaded.</param>
        /// <param name="runtimeBuilder">Runtime builder to use. When null, <see cref="DefaultStObjRuntimeBuilder"/> is used.</param>
        /// <param name="monitor">Optional monitor for loading operation.</param>
        /// <returns>A <see cref="IStObjMap"/> that provides access to the objects graph.</returns>
        public static IStObjMap Load( string assemblyName, IStObjRuntimeBuilder runtimeBuilder = null, IActivityMonitor monitor = null )
        {
            return Load( Assembly.Load( assemblyName ), runtimeBuilder, monitor );
        }

        /// <summary>
        /// Loads a generated assembly according to the configuration.
        /// </summary>
        /// <param name="config">Cofiguration that provides path and name of the assembly to load.</param>
        /// <param name="runtimeBuilder">Runtime builder to use. When null, <see cref="DefaultStObjRuntimeBuilder"/> is used.</param>
        /// <param name="monitor">Optional monitor for loading operation.</param>
        /// <returns>A <see cref="IStObjMap"/> that provides access to the objects graph.</returns>
        public static IStObjMap Load( StObjEngineConfiguration config, IStObjRuntimeBuilder runtimeBuilder = null, IActivityMonitor monitor = null )
        {
            Assembly a = null;
            try
            {
                a = Assembly.Load( BuilderFinalAssemblyConfiguration.GetFinalAssemblyName( config.FinalAssemblyConfiguration.AssemblyName ) );
            }
            catch( Exception ex )
            {
                if( monitor != null ) monitor.Warn().Send( ex, "Unable to load assembly by its name. Trying a LoadFrom its path." );
            }
            if( a == null ) a = Assembly.LoadFrom( config.FinalAssemblyConfiguration.GeneratedAssemblyPath );
            return Load( a, runtimeBuilder, monitor );
        }

        /// <summary>
        /// Loads a previously generated assembly by its assembly name.
        /// </summary>
        /// <param name="assemblyPath">Assembly path that will be loaded (uses <see cref="Assembly.LoadFrom(string)"/>).</param>
        /// <param name="runtimeBuilder">Runtime builder to use. When null, <see cref="DefaultStObjRuntimeBuilder"/> is used.</param>
        /// <param name="monitor">Optional monitor for loading operation.</param>
        /// <returns>A <see cref="IStObjMap"/> that provides access to the objects graph.</returns>
        public static IStObjMap LoadFrom( string assemblyPath, IStObjRuntimeBuilder runtimeBuilder = null, IActivityMonitor monitor = null )
        {
            return Load( Assembly.LoadFrom( assemblyPath ), runtimeBuilder, monitor );
        }

        /// <summary>
        /// Loads a previously generated assembly.
        /// </summary>
        /// <param name="a">Already generated assembly.</param>
        /// <param name="runtimeBuilder">Runtime builder to use. When null, <see cref="DefaultStObjRuntimeBuilder"/> is used.</param>
        /// <param name="monitor">Optional monitor for loading operation.</param>
        /// <returns>A <see cref="IStObjMap"/> that provides access to the objects graph.</returns>
        public static IStObjMap Load( Assembly a, IStObjRuntimeBuilder runtimeBuilder = null, IActivityMonitor monitor = null )
        {
            if( monitor == null ) monitor = new ActivityMonitor( "CK.Core.StObjContextRoot.Load" );
            bool loaded;
            lock( _alreadyLoaded ) 
            {
                loaded = _alreadyLoaded.Contains( a );
                if( !loaded ) _alreadyLoaded.Add( a );
            }
            using( loaded ? null : monitor.OpenInfo().Send( "Loading dynamic '{0}'", a.FullName ) )
            {
                if( a == null ) throw new ArgumentNullException( "a" );
                Type t = a.GetType( RootContextTypeName, true, false );
                return (StObjContextRoot)Activator.CreateInstance( t, new object[] { monitor, runtimeBuilder ?? DefaultStObjRuntimeBuilder } );
            }
        }

        /// <summary>
        /// Runs a build based on the given <paramref name="config"/> object. 
        /// The returned <see cref="StObjBuildResult"/> must be disposed once done with it.
        /// </summary>
        /// <param name="config">Configuration object. It must be serializable.</param>
        /// <param name="builderFactoryStaticMethod">
        /// Must be a static method that returns a <see cref="IStObjRuntimeBuilder"/> or null to use the <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/>.
        /// </param>
        /// <param name="monitor">Optional monitor.</param>
        /// <param name="forceBuild">True to force the build regardless of the stamp.</param>
        /// <returns>A disposable result.</returns>
        public static StObjBuildResult Build( IStObjBuilderConfiguration config, Func<IStObjRuntimeBuilder> builderFactoryStaticMethod = null, IActivityMonitor monitor = null, bool forceBuild = false )
        {
            string typeName = null;
            string methodName = null;
            if( builderFactoryStaticMethod != null )
            {
                var method = builderFactoryStaticMethod.GetMethodInfo();
                if( !method.IsStatic || !method.DeclaringType.GetTypeInfo().IsPublic || !method.IsPublic )
                {
                    throw new ArgumentException( "Must be a public static method of a public class.", "builderFactoryStaticMethod" );
                }
                typeName = builderFactoryStaticMethod.Method.DeclaringType.AssemblyQualifiedName;
                methodName = builderFactoryStaticMethod.Method.Name;
            }
            return DoBuild( config, builderFactoryStaticMethod, typeName, methodName, monitor, forceBuild );
        }

        /// <summary>
        /// Runs a build based on the given <paramref name="config"/> object. 
        /// The returned <see cref="StObjBuildResult"/> must be disposed once done with it.
        /// </summary>
        /// <param name="config">Configuration object. It must be serializable.</param>
        /// <param name="stObjRuntimeBuilderFactoryTypeName">
        /// Assembly qualified name of a type that exposes a static parmeterless method that returns a <see cref="IStObjRuntimeBuilder"/>.
        /// When null, the <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/> is used.
        /// </param>
        /// <param name="stObjRuntimeBuilderFactoryMethodName">Name of the method to call (defaults to "CreateStObjRuntimeBuilder").</param>
        /// <param name="monitor">Optional monitor.</param>
        /// <param name="forceBuild">True to force the build regardless of the stamp.</param>
        /// <returns>A disposable result.</returns>
        public static StObjBuildResult Build(
            IStObjBuilderConfiguration config,
            string stObjRuntimeBuilderFactoryTypeName = null,
            string stObjRuntimeBuilderFactoryMethodName = "CreateStObjRuntimeBuilder",
            IActivityMonitor monitor = null,
            bool forceBuild = false )
        {
            return DoBuild( config, null, stObjRuntimeBuilderFactoryTypeName, stObjRuntimeBuilderFactoryMethodName, monitor, forceBuild );
        }

        static StObjBuildResult DoBuild( 
            IStObjBuilderConfiguration config,
            Func<IStObjRuntimeBuilder> builderMethod,
            string stObjRuntimeBuilderFactoryTypeName, 
            string stObjRuntimeBuilderFactoryMethodName, 
            IActivityMonitor monitor,
            bool forceBuild )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            if( monitor == null ) monitor = new ActivityMonitor( "CK.Core.StObjContextRoot.Build" );

            var stObjConfig = config.StObjEngineConfiguration;
            string stamp = stObjConfig.FinalAssemblyConfiguration.ExternalVersionStamp;
            if( !forceBuild && !string.IsNullOrEmpty( stamp ) )
            {
                string path = stObjConfig.FinalAssemblyConfiguration.GeneratedAssemblyPath;
                if( File.Exists( path ) )
                {
                    //TODO: use Mono.Cecil to load the InformalVersionAttribute, compare it to the stamp
                    //      and sucessfully returns if they are equal.
                }
            }
            IStObjRuntimeBuilder runtimeBuilder = ResolveRuntimeBuilder( builderMethod, stObjRuntimeBuilderFactoryTypeName, stObjRuntimeBuilderFactoryMethodName, monitor );
            return new StObjBuildResult( LaunchRun( monitor, config, runtimeBuilder ), stObjConfig.FinalAssemblyConfiguration.ExternalVersionStamp, false, null );
        }

        static IStObjRuntimeBuilder ResolveRuntimeBuilder( Func<IStObjRuntimeBuilder> builderMethod, string stObjRuntimeBuilderFactoryTypeName, string stObjRuntimeBuilderFactoryMethodName, IActivityMonitor monitor )
        {
            IStObjRuntimeBuilder runtimeBuilder;
            using( monitor.OpenInfo().Send( "Obtention of the IStObjRuntimeBuilder." ) )
            {
                runtimeBuilder = DefaultStObjRuntimeBuilder;
                if( stObjRuntimeBuilderFactoryTypeName != null )
                {
                    if( builderMethod != null ) runtimeBuilder = builderMethod();
                    else
                    {
                        Type t = SimpleTypeFinder.WeakResolver( stObjRuntimeBuilderFactoryTypeName, true );
                        MethodInfo m = t.GetMethod( stObjRuntimeBuilderFactoryMethodName );
                        runtimeBuilder = (IStObjRuntimeBuilder)m.Invoke( null, Util.Array.Empty<object>() );
                    }
                }
                monitor.CloseGroup( runtimeBuilder.GetType().AssemblyQualifiedName );
            }
            return runtimeBuilder;
        }

        static bool LaunchRun( IActivityMonitor monitor, IStObjBuilderConfiguration config, IStObjRuntimeBuilder runtimeBuilder )
        {
            IStObjBuilder runner = (IStObjBuilder)Activator.CreateInstance( SimpleTypeFinder.WeakResolver( config.BuilderAssemblyQualifiedName, true ), monitor, config, runtimeBuilder );
            return runner.Run();
        }
    }
}
