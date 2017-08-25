using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using System.Diagnostics;
using System.Reflection;

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
        StObjEngineConfigurationContext _startConfiguration;

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

        /// <summary>
        /// Gets whether this engine is running or has <see cref="Run"/> (it can run only once).
        /// </summary>
        public bool Started => _startConfiguration != null;

        /// <summary>
        /// Runs the setup.
        /// </summary>
        /// <returns>True on success, false if an error occurred.</returns>
        public bool Run()
        {
            if( _startConfiguration != null ) throw new InvalidOperationException( "Run can be called only once." );
            _startConfiguration = new StObjEngineConfigurationContext( _monitor );
            try
            {
                if( !_startConfiguration.Initialize( _config.Aspects ) ) return false;

                StObjCollectorResult r = SafeBuildStObj();
                if( r == null ) return false;

                var final = new StObjEngineStObjBuildContext( _monitor, _startConfiguration, r.OrderedStObjs );
                return final.RunOnStObjBuild();
            }
            finally
            {
                DisposeDisposableAspects();
            }
        }

        StObjCollectorResult SafeBuildStObj()
        {
            bool hasError = false;
            using( _monitor.OnError( () => hasError = true ) )
            using( _monitor.OpenInfo().Send( "Building StObj objects." ) )
            {
                StObjCollectorResult result;
                AssemblyRegisterer typeReg = new AssemblyRegisterer( _monitor );
                typeReg.Discover( _config.BuildAndRegisterConfiguration.Assemblies );
                var configurator = _startConfiguration.Configurator.BuildConfigurator;
                StObjCollector stObjC = new StObjCollector(
                    _monitor,
                    _config.FinalAssemblyConfiguration,
                    _config.TraceDependencySorterInput,
                    _config.TraceDependencySorterOutput,
                    _runtimeBuilder,
                    configurator, configurator, configurator );
                stObjC.RevertOrderingNames = _config.RevertOrderingNames;
                if( _config.TraceDependencySorterInput ) stObjC.DependencySorterHookInput += i => i.Trace( _monitor );
                if( _config.TraceDependencySorterOutput ) stObjC.DependencySorterHookOutput += i => i.Trace( _monitor );
                stObjC.DependencySorterHookInput += _startConfiguration.StObjDependencySorterHookInput;
                stObjC.DependencySorterHookOutput += _startConfiguration.StObjDependencySorterHookOutput;
                using( _monitor.OpenInfo().Send( "Registering StObj types." ) )
                {
                    stObjC.RegisterTypes( typeReg );
                    stObjC.RegisterClasses( _config.BuildAndRegisterConfiguration.ExplicitClasses );
                    foreach( var t in _startConfiguration.ExplicitRegisteredClasses ) stObjC.RegisterClass( t );
                    Debug.Assert( stObjC.RegisteringFatalOrErrorCount == 0 || hasError, "stObjC.RegisteringFatalOrErrorCount > 0 ==> An error has been logged." );
                }
                if( stObjC.RegisteringFatalOrErrorCount == 0 )
                {
                    using( _monitor.OpenInfo().Send( "Resolving StObj dependency graph." ) )
                    {
                        result = stObjC.GetResult();
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
            foreach( var aspect in _startConfiguration.Aspects.OfType<IDisposable>() )
            {
                try
                {
                    aspect.Dispose();
                }
                catch( Exception ex )
                {
                    _monitor.Error().Send( ex, $"While disposing Aspect '{aspect.GetType().AssemblyQualifiedName}'." );
                }
            }
        }
    }
}
