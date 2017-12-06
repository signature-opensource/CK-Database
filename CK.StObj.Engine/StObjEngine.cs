using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

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
            _status = new Status( _monitor );
            _startContext = new StObjEngineConfigureContext( _monitor, _config, _status );
            try
            {
                _startContext.CreateAndConfigureAspects( _config.Aspects, () => _status.Success = false );
                if( _status.Success )
                {
                    StObjCollectorResult r = SafeBuildStObj();
                    if( r == null ) return _status.Success = false;

                    var runCtx = new StObjEngineRunContext( _monitor, _startContext, r.OrderedStObjs );
                    runCtx.RunAspects( () => _status.Success = false );
                    if( _status.Success )
                    {
                        string directory = _config.FinalAssemblyConfiguration.Directory;
                        if( string.IsNullOrEmpty( directory ) )
                        {
                            directory = AppContext.BaseDirectory;
                            _monitor.Info( $"No directory has been specified for final assembly. Trying to use the AppContext.BaseDirectory path: {directory}" );
                        }
                        string name = _config.GeneratedAssemblyName;
                        if( !name.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) ) name += ".dll";

                        string finalPath = System.IO.Path.Combine( directory, name );
                        _status.Success = r.GenerateFinalAssembly( _monitor, finalPath, _config.GenerateSourceFiles );
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

        StObjCollectorResult SafeBuildStObj()
        {
            bool hasError = false;
            using( _monitor.OnError( () => hasError = true ) )
            using( _monitor.OpenInfo( "Building StObj objects." ) )
            {
                StObjCollectorResult result;
                AssemblyRegisterer typeReg = new AssemblyRegisterer( _monitor );
                typeReg.Discover( _config.BuildAndRegisterConfiguration.Assemblies );
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
                    stObjC.RegisterTypes( typeReg );
                    stObjC.RegisterClasses( _config.BuildAndRegisterConfiguration.ExplicitClasses );
                    foreach( var t in _startContext.ExplicitRegisteredClasses ) stObjC.RegisterClass( t );
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
