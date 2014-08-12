using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection.Emit;
using System.Threading;
using System.Diagnostics;

namespace CK.Core
{
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
        /// <param name="assemblyName">Assembly name that will be loaded in the current AppDomain.</param>
        /// <param name="runtimeBuilder">Runtime builder to use. When null, <see cref="DefaultStObjRuntimeBuilder"/> is used.</param>
        /// <param name="monitor">Optional monitor for loading operation.</param>
        /// <returns>A <see cref="IStObjMap"/> that provides access to the objects graph.</returns>
        public static IStObjMap Load( string assemblyName, IStObjRuntimeBuilder runtimeBuilder = null, IActivityMonitor monitor = null )
        {
            return Load( Assembly.Load( assemblyName ), runtimeBuilder, monitor );
        }

        /// <summary>
        /// Loads a previously generated assembly.
        /// </summary>
        /// <param name="a">Assembly (loaded in the current AppDomain).</param>
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
                Type t = a.GetType( RootContextTypeName, true );
                return (StObjContextRoot)Activator.CreateInstance( t, new object[] { monitor, runtimeBuilder ?? DefaultStObjRuntimeBuilder } );
            }
        }

        /// <summary>
        /// Finds the common ancestor of all the directory in the list. All the path list MUST be rooted.
        /// Returns null if no common ancestor exits.
        /// </summary>
        /// <param name="dirlist">List of directory to analyze</param>
        /// <returns>The common full path</returns>
        public static string FindCommonAncestor( IReadOnlyList<string> dirlist )
        {
            int maxLen;
            string current;
            if( dirlist == null || dirlist.Count == 0 || (current = dirlist[0]) == null || (maxLen = current.Length) == 0 ) return null;
            if( dirlist.Count == 1 ) return current;
            Char cU1 = Char.ToLowerInvariant( current[0] );
            for( int i = 1; i < dirlist.Count; ++i )
            {
                current = dirlist[i];
                int l;
                if( current == null || (l = current.Length) == 0 ) return null;
                if( maxLen > l ) maxLen = l;
                if( Char.ToLowerInvariant( current[0] ) != cU1 ) return null;
            }
            // To be continued with an external loop from 1 to maxLen (to catch chars) and an internal one from 0 to dirlist.Count (for each strings).

            var orderedList = dirlist.OrderBy( x => x );
            DirectoryInfo commonDirectory = orderedList.Select( x => new DirectoryInfo( x ) ).FirstOrDefault();
            string common = null;
            while( common == null && commonDirectory != null )
            {
                if( orderedList.All( x => x.StartsWith( commonDirectory.FullName ) ) )
                {
                    common = commonDirectory.FullName;
                }
                else
                {
                    commonDirectory = commonDirectory.Parent;
                }
            }
            return common;
        }

        enum AppDomainMode
        {
            ForceBuild,
            BuildIfRequired,
            GetVersionStamp,
            ResultFoundExisting
        }

        class AppDomainCommunication : MarshalByRefObject
        {
            readonly object _locker = new object();
            bool _done;
            bool _success;

            public AppDomainCommunication( IActivityMonitor monitor, IStObjEngineConfiguration config, AppDomainMode m, string stObjRuntimeBuilderFactoryTypeName, string stObjRuntimeBuilderFactoryMethodName )
            {
                if( !config.GetType().IsSerializable ) throw new InvalidOperationException( "IStObjEngineConfiguration implementation must be serializable." );
                _locker = new object();
                LoggerBridge = monitor.Output.BridgeTarget;
                Config = config;
                if( m != AppDomainMode.GetVersionStamp ) VersionStampRead = config.FinalAssemblyConfiguration.ExternalVersionStamp;
                StObjRuntimeBuilderFactoryTypeName = stObjRuntimeBuilderFactoryTypeName;
                StObjRuntimeBuilderFactoryMethodName = stObjRuntimeBuilderFactoryMethodName;
                Mode = m;
            }

            public string VersionStampRead { get; set; }

            public AppDomainMode Mode { get; set; }

            public ActivityMonitorBridgeTarget LoggerBridge { get; private set; }

            public IStObjEngineConfiguration Config { get; private set; }

            public string StObjRuntimeBuilderFactoryTypeName { get; private set; }

            public string StObjRuntimeBuilderFactoryMethodName { get; private set; }

            public bool WaitForResult()
            {
                lock( _locker )
                    while( !_done )
                        Monitor.Wait( _locker );
                return _success;
            }

            public void SetResult( bool success )
            {
                _success = success;
                lock( _locker )
                {
                    _done = true;
                    Monitor.Pulse( _locker );
                }
            }
        }


        /// <summary>
        /// Runs a build based on the given serializable <paramref name="config"/> object. 
        /// The returned <see cref="StObjBuildResult"/> must be disposed once done with it.
        /// </summary>
        /// <param name="config">Configuration object. It must be serializable.</param>
        /// <param name="stObjRuntimeBuilderFactoryTypeName">
        /// Assembly qualified name of a public type that exposes a factory method of the <see cref="IStObjRuntimeBuilder"/> that must be used.
        /// The method must be public and static.
        /// When null, the <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/> is used.
        /// </param>
        /// <param name="stObjRuntimeBuilderFactoryMethodName">Name of the method to call (defaults to "CreateStObjRuntimeBuilder").</param>
        /// <param name="builderFactoryStaticMethod">
        /// Must be a static method that returns a <see cref="IStObjRuntimeBuilder"/> or null to use the <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/>.
        /// </param>
        /// <param name="monitor">Optional monitor.</param>
        /// <returns>A disposable result.</returns>
        public static StObjBuildResult Build( IStObjEngineConfiguration config, Func<IStObjRuntimeBuilder> builderFactoryStaticMethod = null, IActivityMonitor monitor = null, bool forceBuild = false )
        {
            string typeName = null;
            string methodName = null;
            if( builderFactoryStaticMethod != null )
            {
                if( !builderFactoryStaticMethod.Method.IsStatic || !builderFactoryStaticMethod.Method.DeclaringType.IsPublic || !builderFactoryStaticMethod.Method.IsPublic )
                {
                    throw new ArgumentException( "Must be a public static method of a public class.", "builderFactoryStaticMethod" );
                }
                typeName = builderFactoryStaticMethod.Method.DeclaringType.AssemblyQualifiedName;
                methodName = builderFactoryStaticMethod.Method.Name;
            }
            return DoBuild( config, builderFactoryStaticMethod, typeName, methodName, monitor, forceBuild );
        }

        /// <summary>
        /// Runs a build based on the given serializable <paramref name="config"/> object. 
        /// The returned <see cref="StObjBuildResult"/> must be disposed once done with it.
        /// </summary>
        /// <param name="config">Configuration object. It must be serializable.</param>
        /// <param name="stObjRuntimeBuilderFactoryTypeName">
        /// Assembly qualified name of a type that exposes a static parmeterless method that retunrs a <see cref="IStObjRuntimeBuilder"/>.
        /// When null, the <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/> is used.
        /// </param>
        /// <param name="stObjRuntimeBuilderFactoryMethodName">Name of the method to call (defaults to "CreateStObjRuntimeBuilder").</param>
        /// <param name="monitor">Optional monitor.</param>
        /// <returns>A disposable result.</returns>
        public static StObjBuildResult Build(
            IStObjEngineConfiguration config,
            string stObjRuntimeBuilderFactoryTypeName = null,
            string stObjRuntimeBuilderFactoryMethodName = "CreateStObjRuntimeBuilder",
            IActivityMonitor monitor = null,
            bool forceBuild = false )
        {
            return DoBuild( config, null, stObjRuntimeBuilderFactoryTypeName, stObjRuntimeBuilderFactoryMethodName, monitor, forceBuild );
        }

        static StObjBuildResult DoBuild( 
            IStObjEngineConfiguration config,
            Func<IStObjRuntimeBuilder> builderMethod,
            string stObjRuntimeBuilderFactoryTypeName, 
            string stObjRuntimeBuilderFactoryMethodName, 
            IActivityMonitor monitor, 
            bool forceBuild )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            if( monitor == null ) monitor = new ActivityMonitor( "CK.Core.StObjContextRoot.Build" );

            StObjBuildResult r = null;
            if( config.AppDomainConfiguration.UseIndependentAppDomain && !config.AppDomainConfiguration.Assemblies.IsEmptyConfiguration )
            {
                using( monitor.OpenInfo().Send( "Build process. Creating an independent AppDomain." ) )
                {
                    r = BuildOrGetVersionStampInIndependentAppDomain( 
                                config, 
                                stObjRuntimeBuilderFactoryTypeName, 
                                stObjRuntimeBuilderFactoryMethodName, 
                                monitor, 
                                forceBuild ? AppDomainMode.ForceBuild : AppDomainMode.BuildIfRequired );
                }
            }
            else
            {
                if( !forceBuild && config.FinalAssemblyConfiguration.ExternalVersionStamp != null )
                {
                    using( monitor.OpenInfo().Send( "Checking potentially existing generated dll ExternalVersionStamp in an independent AppDomain." ) )
                    {
                        // Extracts the Version stamp of the existing dll (if any) in an independent AppDomain to
                        // avoid cluttering the ReflectionOnly context of the current AppDomain.
                        r = BuildOrGetVersionStampInIndependentAppDomain( 
                                    config,
                                    stObjRuntimeBuilderFactoryTypeName,
                                    stObjRuntimeBuilderFactoryMethodName,
                                    monitor, 
                                    AppDomainMode.GetVersionStamp );
                        if( !r.Success || r.ExternalVersionStamp != config.FinalAssemblyConfiguration.ExternalVersionStamp )
                        {
                            monitor.Info().Send( "Build is required." );
                            r.Dispose();
                            r = null;
                        }
                        else
                        {
                            monitor.Info().Send( "Generated dll exist with the exact Version stamp. Building it again is useless." );
                        }
                    }
                }
                if( r == null )
                {
                    IStObjRuntimeBuilder runtimeBuilder = ResolveRuntimeBuilder( builderMethod, stObjRuntimeBuilderFactoryTypeName, stObjRuntimeBuilderFactoryMethodName, monitor );
                    r = new StObjBuildResult( LaunchRun( monitor, config, runtimeBuilder ), config.FinalAssemblyConfiguration.ExternalVersionStamp, false, null, null );
                }
            }
            return r;
        }

        private static IStObjRuntimeBuilder ResolveRuntimeBuilder( Func<IStObjRuntimeBuilder> builderMethod, string stObjRuntimeBuilderFactoryTypeName, string stObjRuntimeBuilderFactoryMethodName, IActivityMonitor monitor )
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
                        Type t = SimpleTypeFinder.WeakDefault.ResolveType( stObjRuntimeBuilderFactoryTypeName, true );
                        MethodInfo m = t.GetMethod( stObjRuntimeBuilderFactoryMethodName );
                        runtimeBuilder = (IStObjRuntimeBuilder)m.Invoke( null, Util.EmptyArray<object>.Empty );
                    }
                }
            }
            return runtimeBuilder;
        }

        private static bool LaunchRun( IActivityMonitor monitor, IStObjEngineConfiguration config, IStObjRuntimeBuilder runtimeBuilder )
        {
            monitor.Info().Send( "Current AppDomain.CurrentDomain.FriendlyName = '{0}'.", AppDomain.CurrentDomain.FriendlyName );
            IStObjBuilder runner = (IStObjBuilder)Activator.CreateInstance( SimpleTypeFinder.WeakDefault.ResolveType( config.BuilderAssemblyQualifiedName, true ), monitor, config, runtimeBuilder );
            return runner.Run();
        }

        private static StObjBuildResult BuildOrGetVersionStampInIndependentAppDomain( 
            IStObjEngineConfiguration config,
            string stObjRuntimeBuilderFactoryTypeName,
            string stObjRuntimeBuilderFactoryMethodName,
            IActivityMonitor monitor, 
            AppDomainMode m )
        {
            AppDomainSetup thisSetup = AppDomain.CurrentDomain.SetupInformation;
            AppDomainSetup setup = new AppDomainSetup();

            if( m == AppDomainMode.GetVersionStamp )
            {
                setup.ApplicationBase = thisSetup.ApplicationBase;
                setup.PrivateBinPathProbe = thisSetup.PrivateBinPathProbe;
                setup.PrivateBinPath = thisSetup.PrivateBinPath;
            }
            else
            {
                var result = FindCommonAncestor( config.AppDomainConfiguration.ProbePaths );
                if( result == null )
                {
                    throw new CKException( "All the probe paths must have a common ancestor. No ancestor found for: '{0}'.", string.Join( "', '", config.AppDomainConfiguration.ProbePaths ) );
                }
                setup.ApplicationBase = result;
                /// PrivateBinPathProbe (from msdn):
                /// Set this property to any non-null string value, including String.Empty (""), to exclude the application directory path — that is, 
                /// ApplicationBase — from the search path for the application, and to search for assemblies only in PrivateBinPath. 
                setup.PrivateBinPathProbe = String.Empty;
                setup.PrivateBinPath = string.Join( ";", config.AppDomainConfiguration.ProbePaths );
            }
            var appDomain = AppDomain.CreateDomain( "StObjContextRoot.Build.IndependentAppDomain", null, setup );
            AppDomainCommunication appDomainComm = new AppDomainCommunication( monitor, config, m, stObjRuntimeBuilderFactoryTypeName, stObjRuntimeBuilderFactoryMethodName );
            appDomain.SetData( "CK-AppDomainComm", appDomainComm );
            appDomain.DoCallBack( new CrossAppDomainDelegate( LaunchRunCrossDomain ) );
            return new StObjBuildResult( appDomainComm.WaitForResult(), appDomainComm.VersionStampRead, appDomainComm.Mode == AppDomainMode.ResultFoundExisting, appDomain, monitor );
        }

        private static void LaunchRunCrossDomain()
        {
            AppDomainCommunication appDomainComm = (AppDomainCommunication)AppDomain.CurrentDomain.GetData( "CK-AppDomainComm" );
            var config = appDomainComm.Config;
            IActivityMonitor monitor = new ActivityMonitor( false );
            using( monitor.Output.CreateStrongBridgeTo( appDomainComm.LoggerBridge ) )
            try
            {
                string existingVersionStamp = null;
                if( appDomainComm.Mode == AppDomainMode.GetVersionStamp
                    || (appDomainComm.Mode == AppDomainMode.BuildIfRequired && config.FinalAssemblyConfiguration.ExternalVersionStamp != null) )
                {
                    // If no directory has been specified for final assembly. Trying to use the path of CK.StObj.Model assembly.
                    // If no assembly name has been specified for final assembly. Using default name.
                    // ==> This mimics GenerateFinalAssembly behavior.
                    string directory = config.FinalAssemblyConfiguration.Directory;
                    if( String.IsNullOrEmpty( directory ) ) directory = BuilderFinalAssemblyConfiguration.GetFinalDirectory( directory );
                    string assemblyName = config.FinalAssemblyConfiguration.AssemblyName;
                    if( String.IsNullOrEmpty( assemblyName ) ) assemblyName = BuilderFinalAssemblyConfiguration.GetFinalAssemblyName( assemblyName );

                    string p = Path.Combine( directory, assemblyName + ".dll" );
                    try
                    {
                        if( File.Exists( p ) )
                        {
                            Assembly a = Assembly.ReflectionOnlyLoadFrom( p );
                            foreach( var attr in a.GetCustomAttributesData() )
                            {
                                if( typeof( AssemblyInformationalVersionAttribute ).IsAssignableFrom( attr.Constructor.DeclaringType ) )
                                {
                                    if( attr.ConstructorArguments.Count > 0 ) existingVersionStamp = attr.ConstructorArguments[0].Value as string;
                                    break;
                                }
                            }
                            if( appDomainComm.Mode == AppDomainMode.BuildIfRequired )
                            {
                                if( existingVersionStamp == config.FinalAssemblyConfiguration.ExternalVersionStamp )
                                {
                                    monitor.Info().Send( "File '{0}' already exists with the expected Version stamp. Building it again is useless.", p );
                                    appDomainComm.Mode = AppDomainMode.ResultFoundExisting;
                                }
                                else monitor.Trace().Send( "File '{0}' already exists but Version stamp differs. Building is required.", p );
                            }
                            else if( appDomainComm.Mode == AppDomainMode.GetVersionStamp )
                            {
                                if( existingVersionStamp != null )
                                {
                                    monitor.Info().Send( "File '{0}' already exists. Its Version stamp has been extracted ('{1}').", p, existingVersionStamp );
                                    appDomainComm.Mode = AppDomainMode.ResultFoundExisting;
                                }
                            }
                            appDomainComm.VersionStampRead = existingVersionStamp;
                        }
                        else monitor.Trace().Send( "File '{0}' does not exist.", p );
                    }
                    catch( Exception ex )
                    {
                        monitor.Error().Send( ex, "While trying to read version stamp from '{0}'.", p );
                    }
                }
                // Conclusion: if a build is required, run it, otherwise if a version has been read, it is a success.
                if( appDomainComm.Mode == AppDomainMode.ResultFoundExisting || appDomainComm.Mode == AppDomainMode.GetVersionStamp )
                {
                    appDomainComm.SetResult( existingVersionStamp != null );
                }
                else
                {
                    IStObjRuntimeBuilder runtimeBuilder = ResolveRuntimeBuilder( null, appDomainComm.StObjRuntimeBuilderFactoryTypeName, appDomainComm.StObjRuntimeBuilderFactoryMethodName, monitor );
                    // Updates the VersionStampRead on the output.
                    appDomainComm.VersionStampRead = config.FinalAssemblyConfiguration.ExternalVersionStamp;
                    appDomainComm.SetResult( LaunchRun( monitor, config, runtimeBuilder ) );
                }
            }
            catch( Exception ex )
            {
                monitor.Fatal().Send( ex );
                appDomainComm.SetResult( false );
            }
        }

    }
}
