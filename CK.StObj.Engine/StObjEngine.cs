using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using System.IO;
using CK.Text;

namespace CK.Setup
{
    /// <summary>
    /// Generic engine that runs a <see cref="StObjEngineConfiguration"/>.
    /// </summary>
    public class StObjEngine
    {
        readonly IActivityMonitor _monitor;
        readonly StObjEngineConfiguration _config;
        readonly IStObjRuntimeBuilder _runtimeBuilder;
        Status _status;
        StObjEngineConfigureContext _startContext;

        class Status : IStObjEngineStatus, IDisposable
        {
            readonly IActivityMonitor _m;
            readonly ActivityMonitorPathCatcher _pathCatcher;
            public bool Success;

            public Status( IActivityMonitor m )
            {
                _m = m;
                _pathCatcher = new ActivityMonitorPathCatcher() { IsLocked = true };
                _m.Output.RegisterClient( _pathCatcher );
                Success = true;
            }

            bool IStObjEngineStatus.Success => Success;

            public IReadOnlyList<ActivityMonitorPathCatcher.PathElement> DynamicPath => _pathCatcher.DynamicPath;

            public IReadOnlyList<ActivityMonitorPathCatcher.PathElement> LastErrorPath => _pathCatcher.LastErrorPath;

            public IReadOnlyList<ActivityMonitorPathCatcher.PathElement> LastWarnOrErrorPath => _pathCatcher.LastErrorPath;

            public void Dispose()
            {
                _pathCatcher.IsLocked = false;
                _m.Output.UnregisterClient( _pathCatcher );
            }
        }

        /// <summary>
        /// Initializes a new <see cref="StObjEngine"/>.
        /// </summary>
        /// <param name="monitor">Logger that must be used.</param>
        /// <param name="config">Configuration that describes the key aspects of the build.</param>
        /// <param name="runtimeBuilder">The object in charge of actual objects instanciation. When null, <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/> is used.</param>
        public StObjEngine( IActivityMonitor monitor, StObjEngineConfiguration config, IStObjRuntimeBuilder runtimeBuilder = null )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( config == null ) throw new ArgumentNullException( nameof( config ) );
            _monitor = monitor;
            _config = config;
            _runtimeBuilder = runtimeBuilder ?? StObjContextRoot.DefaultStObjRuntimeBuilder;
        }

        ///// <summary>
        ///// Initializes a new <see cref="StObjEngine"/>.
        ///// </summary>
        ///// <param name="monitor">Logger that must be used.</param>
        ///// <param name="config">Configuration that describes the key aspects of the build.</param>
        //public StObjEngine( IActivityMonitor monitor, XElement config )
        //{
        //    if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
        //    if( config == null ) throw new ArgumentNullException( nameof( config ) );
        //    _monitor = monitor;
        //    _config = new StObjEngineConfiguration( config );
        //    _runtimeBuilder = StObjContextRoot.DefaultStObjRuntimeBuilder;
        //}

        struct NormalizedFolder
        {
            public readonly string Directory;
            public readonly HashSet<string> Assemblies;
            public readonly HashSet<string> ExplicitTypes;

            public NormalizedFolder( string d, HashSet<string> a, HashSet<string> t )
            {
                Directory = d;
                Assemblies = a;
                ExplicitTypes = t;
            }
        }

        /// <summary>
        /// Gets whether this engine is running or has <see cref="Run"/> (it can run only once).
        /// </summary>
        public bool Started => _startContext != null;

        /// <summary>
        /// Runs the setup.
        /// </summary>
        /// <returns>True on success, false if an error occurred.</returns>
        public bool Run()
        {
            if( _startContext != null ) throw new InvalidOperationException( "Run can be called only once." );
            List<NormalizedFolder> normalizedFolders = NormalizeConfiguration();
            if( normalizedFolders == null ) return false; 
            _status = new Status( _monitor );
            _startContext = new StObjEngineConfigureContext( _monitor, _config, _status );
            try
            {
                _startContext.CreateAndConfigureAspects( _config.Aspects, () => _status.Success = false );
                if( _status.Success )
                {
                    StObjCollectorResult r = SafeBuildStObj( normalizedFolders[0] );
                    if( r == null ) return _status.Success = false;

                    var runCtx = new StObjEngineRunContext( _monitor, _startContext, r.OrderedStObjs );
                    runCtx.RunAspects( () => _status.Success = false );
                    if( _status.Success )
                    {
                        string dllName = _config.GeneratedAssemblyName;
                        if( !dllName.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) ) dllName += ".dll";

                        if( _config.GenerateAppContextAssembly )
                        {
                            using( _monitor.OpenInfo( "Generating AppContext (global) assembly." ) )
                            {
                                string finalPath = Path.Combine( AppContext.BaseDirectory, dllName );
                                _status.Success = r.GenerateFinalAssembly( _monitor, finalPath, _config.GenerateSourceFiles );
                            }
                        }
                        if( _status.Success )
                        {

                        }
                    }
                    if( !_status.Success )
                    {
                        var errorPath = _status.LastErrorPath;
                        if( errorPath == null || errorPath.Count == 0 )
                        {
                            Debug.Fail( "Success status is false but no error has been logged." );
                            _monitor.Fatal( "Success status is false but no error has been logged." );
                        }
                    }
                    var termCtx = new StObjEngineTerminateContext( _monitor, runCtx );
                    termCtx.TerminateAspects( () => _status.Success = false );
                }
                return _status.Success;
            }
            finally
            {
                DisposeDisposableAspects();
                _status.Dispose();
            }
        }

        List<NormalizedFolder> NormalizeConfiguration()
        {
            using( _monitor.OpenInfo( "Validating configuration." ) )
            {
                if( _config.Assemblies.Count == 0 && _config.Types.Count == 0 ) _monitor.Error( "Assemblies and ExplicitClasses are empty." );
                else
                {
                    var normalized = new List<NormalizedFolder>();
                    string baseDir = FileUtil.NormalizePathSeparator( AppContext.BaseDirectory, true );
                    normalized.Add( new NormalizedFolder( baseDir, _config.Assemblies, _config.Types ) );
                    foreach( var f in _config.SetupFolders )
                    {
                        if( f == null ) _monitor.Error( "A null SetupFolder found in SetupFolders." );
                        else
                        {
                            try
                            {
                                string n = FileUtil.NormalizePathSeparator( Path.GetFullPath( f.Directory ), true );
                                if( !Directory.Exists( n ) ) _monitor.Error( $"Directory '{n}' does not exist." );
                                else
                                {
                                    var clash = normalized.FirstOrDefault( norm => n.StartsWith( norm.Directory, StringComparison.OrdinalIgnoreCase )
                                                                                   || norm.Directory.StartsWith( n, StringComparison.OrdinalIgnoreCase ) );
                                    if( clash.Directory != null )
                                    {
                                        _monitor.Error( $"Directory '{n}' can not be below other SetupFolder '{clash.Directory}'." );
                                    }
                                    else
                                    {
                                        bool ok = true;
                                        var aliens = f.Assemblies.Except( _config.Assemblies );
                                        if( aliens.Any() )
                                        {
                                            _monitor.Error( $"SetupFolder '{n}' contains at least one assembly that is not in global configuration: {aliens.Concatenate()}" );
                                            ok = false;
                                        }
                                        aliens = f.Types.Except( _config.Types );
                                        if( aliens.Any() )
                                        {
                                            _monitor.Error( $"SetupFolder '{n}' contains at least one explicit class that is not in global configuration: {aliens.Concatenate()}" );
                                            ok = false;
                                        }
                                        if( ok )
                                        {
                                            normalized.Add( new NormalizedFolder( n, f.Assemblies, f.Types ) );
                                        }
                                    }
                                }
                            }
                            catch( Exception ex )
                            {
                                _monitor.Error( $"Invalid SetupFolder.Directory.", ex );
                            }
                        }
                    }
                    if( normalized.Count == _config.SetupFolders.Count + 1 )
                    {
                        return normalized;
                    }
                }
            }
            return null;
        }

        StObjCollectorResult SafeBuildStObj( NormalizedFolder f )
        {
            bool hasError = false;
            using( _monitor.OnError( () => hasError = true ) )
            using( _monitor.OpenInfo( "Building StObj objects." ) )
            {
                StObjCollectorResult result;
                var configurator = _startContext.Configurator.FirstLayer;
                StObjCollector stObjC = new StObjCollector(
                    _monitor,
                    _config.TraceDependencySorterInput,
                    _config.TraceDependencySorterOutput,
                    _runtimeBuilder,
                    configurator, configurator, configurator );
                stObjC.RevertOrderingNames = _config.RevertOrderingNames;
                if( _config.TraceDependencySorterInput ) stObjC.DependencySorterHookInput += i => i.Trace( _monitor );
                if( _config.TraceDependencySorterOutput ) stObjC.DependencySorterHookOutput += i => i.Trace( _monitor );
                stObjC.DependencySorterHookInput += _startContext.StObjDependencySorterHookInput;
                stObjC.DependencySorterHookOutput += _startContext.StObjDependencySorterHookOutput;
                using( _monitor.OpenInfo( "Registering StObj types." ) )
                {
                    stObjC.RegisterAssemblyTypes( f.Assemblies );
                    stObjC.RegisterTypes( f.ExplicitTypes );
                    foreach( var t in _startContext.ExplicitRegisteredTypes ) stObjC.RegisterType( t );
                    Debug.Assert( stObjC.RegisteringFatalOrErrorCount == 0 || hasError, "stObjC.RegisteringFatalOrErrorCount > 0 ==> An error has been logged." );
                }
                if( stObjC.RegisteringFatalOrErrorCount == 0 )
                {
                    using( _monitor.OpenInfo( "Resolving StObj dependency graph." ) )
                    {
                        result = stObjC.GetResult( _startContext.ServiceContainer );
                        Debug.Assert( !result.HasFatalError || hasError, "result.HasFatalError ==> An error has been logged." );
                    }
                    if( !result.HasFatalError ) return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Disposes all disposable aspects.
        /// </summary>
        void DisposeDisposableAspects()
        {
            foreach( var aspect in _startContext.Aspects.OfType<IDisposable>() )
            {
                try
                {
                    aspect.Dispose();
                }
                catch( Exception ex )
                {
                    _monitor.Error( $"While disposing Aspect '{aspect.GetType().AssemblyQualifiedName}'.", ex );
                }
            }
        }

    }
}
