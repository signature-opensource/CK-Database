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
        /// <summary>
        /// Holds the name of 'Construct' method.
        /// </summary>
        public static readonly string ConstructMethodName = "StObjConstruct";

        /// <summary>
        /// Holds the name of 'Initialize' method.
        /// </summary>
        public static readonly string InitializeMethodName = "StObjInitialize";

        static readonly HashSet<Assembly> _alreadyLoaded = new HashSet<Assembly>();

        /// <summary>
        /// Loads a previously generated assembly.
        /// </summary>
        /// <param name="a">Already generated assembly.</param>
        /// <param name="runtimeBuilder">Runtime builder to use. When null, <see cref="DefaultStObjRuntimeBuilder"/> is used.</param>
        /// <param name="monitor">Optional monitor for loading operation.</param>
        /// <returns>A <see cref="IStObjMap"/> that provides access to the objects graph.</returns>
        public static IStObjMap Load( Assembly a, IStObjRuntimeBuilder runtimeBuilder = null, IActivityMonitor monitor = null )
        {
            if( a == null ) throw new ArgumentNullException( nameof( a ) );
            IActivityMonitor m = monitor ?? new ActivityMonitor( "CK.Core.StObjContextRoot.Load" );
            bool loaded;
            lock( _alreadyLoaded )
            {
                loaded = _alreadyLoaded.Contains( a );
                if( !loaded ) _alreadyLoaded.Add( a );
            }
            using( m.OpenInfo( loaded ? $"'{a.FullName}' is already loaded." : $"Loading dynamic '{a.FullName}'." ) )
            {
                try
                {
                    Type t = a.GetType( RootContextTypeName, true, false );
                    return (IStObjMap)Activator.CreateInstance( t, new object[] { m, runtimeBuilder ?? DefaultStObjRuntimeBuilder } );
                }
                catch( Exception ex )
                {
                    m.Error( "Unable to instanciate StObjMap.", ex );
                    return null;
                }
                finally
                {
                    m.CloseGroup();
                    if( monitor == null ) m.MonitorEnd();
                }
            }
        }

        /// <summary>
        /// Runs a build based on the given <paramref name="config"/> object. 
        /// </summary>
        /// <param name="config">Configuration object. It must be serializable.</param>
        /// <param name="builderFactoryStaticMethod">
        /// Must be a static method that returns a <see cref="IStObjRuntimeBuilder"/> or null to use the <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/>.
        /// </param>
        /// <param name="monitor">Optional monitor.</param>
        /// <returns>True on success, false if build has failed.</returns>
        public static bool Build( StObjEngineConfiguration config, Func<IStObjRuntimeBuilder> builderFactoryStaticMethod = null, IActivityMonitor monitor = null )
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
                typeName = method.DeclaringType.AssemblyQualifiedName;
                methodName = method.Name;
            }
            return DoBuild( config, builderFactoryStaticMethod, typeName, methodName, monitor );
        }

        /// <summary>
        /// Runs a build based on the given <paramref name="config"/> object. 
        /// </summary>
        /// <param name="config">Configuration object. It must be serializable.</param>
        /// <param name="stObjRuntimeBuilderFactoryTypeName">
        /// Assembly qualified name of a type that exposes a static parmeterless method that returns a <see cref="IStObjRuntimeBuilder"/>.
        /// When null, the <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/> is used.
        /// </param>
        /// <param name="stObjRuntimeBuilderFactoryMethodName">Name of the method to call (defaults to "CreateStObjRuntimeBuilder").</param>
        /// <param name="monitor">Optional monitor.</param>
        /// <returns>True on success, false if the build faield.</returns>
        public static bool Build(
            StObjEngineConfiguration config,
            string stObjRuntimeBuilderFactoryTypeName = null,
            string stObjRuntimeBuilderFactoryMethodName = "CreateStObjRuntimeBuilder",
            IActivityMonitor monitor = null )
        {
            return DoBuild( config, null, stObjRuntimeBuilderFactoryTypeName, stObjRuntimeBuilderFactoryMethodName, monitor );
        }

        static bool DoBuild(
            StObjEngineConfiguration config,
            Func<IStObjRuntimeBuilder> builderMethod,
            string stObjRuntimeBuilderFactoryTypeName,
            string stObjRuntimeBuilderFactoryMethodName,
            IActivityMonitor monitor )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            if( monitor == null ) monitor = new ActivityMonitor( "CK.Core.StObjContextRoot.Build" );

            IStObjRuntimeBuilder runtimeBuilder = ResolveRuntimeBuilder( builderMethod, stObjRuntimeBuilderFactoryTypeName, stObjRuntimeBuilderFactoryMethodName, monitor );
            Type runnerType = SimpleTypeFinder.WeakResolver( config.EngineAssemblyQualifiedName, true );
            object runner = Activator.CreateInstance( runnerType, monitor, config, runtimeBuilder );
            MethodInfo m = runnerType.GetMethod( "Run" );
            return (bool)m.Invoke( runner, Array.Empty<object>() );
        }

        static IStObjRuntimeBuilder ResolveRuntimeBuilder( Func<IStObjRuntimeBuilder> builderMethod, string stObjRuntimeBuilderFactoryTypeName, string stObjRuntimeBuilderFactoryMethodName, IActivityMonitor monitor )
        {
            IStObjRuntimeBuilder runtimeBuilder;
            using( monitor.OpenInfo( "Obtention of the IStObjRuntimeBuilder." ) )
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

    }
}
