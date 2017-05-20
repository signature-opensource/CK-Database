using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CK.Core;

namespace CK.Setup
{
    public sealed partial class SetupEngine : IStObjBuilder
    {
        readonly SetupEngineConfiguration _config;
        readonly SetupEngineConfigurator _configurator;
        readonly IActivityMonitor _monitor;
        readonly SetupEngineStartConfiguration _startConfiguration;
        readonly IStObjRuntimeBuilder _runtimeBuilder;
        readonly EventHandler<RegisterSetupEventArgs> _relayRegisterSetupEvent;
        readonly EventHandler<SetupEventArgs> _relaySetupEvent;
        readonly EventHandler<DriverEventArgs> _relayDriverEvent;
        bool _started;

        /// <summary>
        /// Initializes a new <see cref="SetupEngine"/>. This constructor is the one used when calling <see cref="StObjBuilder.SafeBuildStObj"/> method 
        /// with a <see cref="SetupEngineConfiguration"/> configuration object.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="config">Configuration object.</param>
        /// <param name="runtimeBuilder">Final builder of objects.</param>
        public SetupEngine( IActivityMonitor monitor, SetupEngineConfiguration config, IStObjRuntimeBuilder runtimeBuilder )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( config == null ) throw new ArgumentNullException( "config" );
            _monitor = monitor;
            _config = config;
            _configurator = new SetupEngineConfigurator();
            _startConfiguration = new SetupEngineStartConfiguration( this );
            _runtimeBuilder = runtimeBuilder;
            _relayRegisterSetupEvent = OnEngineRegisterSetupEvent;
            _relaySetupEvent = OnEngineSetupEvent;
            _relayDriverEvent = OnEngineDriverEvent;
        }

        /// <summary>
        /// Gets whether this engine is running or has <see cref="Run"/> (it can run only once).
        /// </summary>
        public bool Started => _started; 

        /// <summary>
        /// Gets the monitor that should be used for the whole setup process.
        /// </summary>
        public IActivityMonitor Monitor => _monitor; 

        /// <summary>
        /// Gets the configuration object of this engine.
        /// </summary>
        public SetupEngineConfiguration Configuration => _config; 

        /// <summary>
        /// Gets or sets a <see cref="SetupableConfigurator"/> that will be used.
        /// This can be changed at any moment during setup: the current configurator will always be used.
        /// When setting it, care should be taken to not break the chain by setting the current configurator as the <see cref="SetupEngineConfigurator.Previous"/>.
        /// </summary>
        public SetupEngineConfigurator SetupableConfigurator
        {
            get { return _configurator.Previous; }
            set { _configurator.Previous = value; }
        }

        /// <summary>
        /// Gets the <see cref="SetupEngineStartConfiguration"/> object.
        /// </summary>
        public SetupEngineStartConfiguration StartConfiguration => _startConfiguration; 

        /// <summary>
        /// Triggered before registration (at the beginning of <see cref="SetupCoreEngine.RegisterAndCreateDrivers"/>).
        /// This event fires before the <see cref="SetupEvent"/> (with <see cref="SetupEventArgs.Step"/> set to None), and enables
        /// registration of setup items.
        /// </summary>
        public event EventHandler<RegisterSetupEventArgs> RegisterSetupEvent;

        /// <summary>
        /// Triggered for each steps of <see cref="SetupStep"/>: None (before registration), Init, Install, Settle and Done.
        /// </summary>
        public event EventHandler<SetupEventArgs> SetupEvent;

        /// <summary>
        /// Triggered for each <see cref="DriverBase"/> setup phases.
        /// </summary>
        public event EventHandler<DriverEventArgs> DriverEvent;       

        /// <summary>
        /// Executes the whole setup process (<see cref="SetupCoreEngine.RegisterAndCreateDrivers"/>, <see cref="SetupCoreEngine.RunInit"/>, <see cref="SetupCoreEngine.RunInstall"/>, <see cref="SetupCoreEngine.RunSettle"/>).
        /// This is automatically called by  <see cref="StObjBuilder.SafeBuildStObj(SetupEngine, IStObjRuntimeBuilder, SetupEngineConfigurator)"/> after it has instanciating this object when using a <see cref="SetupEngineConfiguration"/>.
        /// This can be called only once.
        /// </summary>
        /// <returns>True on success, false if an error occured.</returns>
        public bool Run()
        {
            return ManualRun();
        }

        /// <summary>
        /// Creates the configured aspects, resolves and builds the StObj graph and registers any number of <see cref="IDependentItem"/> and/or <see cref="IDependentItemDiscoverer"/> 
        /// and/or <see cref="IEnumerable"/> of such objects (recursively) and executes the whole setup process (<see cref="SetupCoreEngine.RegisterAndCreateDrivers"/>, <see cref="SetupCoreEngine.RunInit"/>, <see cref="SetupCoreEngine.RunInstall"/>, <see cref="SetupCoreEngine.RunSettle"/>).
        /// This can be called only once.
        /// </summary>
        /// <param name="items">Objects that can be <see cref="IDependentItem"/>, <see cref="IDependentItemDiscoverer"/> or both and/or <see cref="IEnumerable"/> of such objects (recursively).</param>
        /// <returns>True on success, false if an error occured.</returns>
        public bool ManualRun( params object[] items )
        {
            if( _started ) throw new InvalidOperationException( "Run or ManualRun can be called only once." );
            _started = true;
            try
            {
                if( !CreateEngineAspectsFromConfiguration() ) return false;
                if( _startConfiguration.VersionedItemReader == null ) throw new InvalidOperationException( "StartConfiguration.VersionedItemReader must be set before calling Run or ManualRun." );
                if( _startConfiguration.VersionedItemWriter == null ) throw new InvalidOperationException( "StartConfiguration.VersionedItemWriter must be set before calling Run or ManualRun." );
                if( _startConfiguration.SetupSessionMemoryProvider == null ) throw new InvalidOperationException( "StartConfiguration.SetupSessionMemoryProvider must be set before calling Run or ManualRun." );
                if( _config.RunningMode == SetupEngineRunningMode.InitializeEngineOnly )
                {
                    _monitor.Info().Send( "RunningMode = InitializeAspectsOnly complete." );
                    return true;
                }
                var buildResult = StObjBuilder.SafeBuildStObj( this, _runtimeBuilder, _configurator );
                if( buildResult == null ) return false;
                var path = _monitor.Output.RegisterClient( new ActivityMonitorPathCatcher() );
                var memoryProvider = _startConfiguration.SetupSessionMemoryProvider;
                ISetupSessionMemory m = null;
                try
                {
                    m = memoryProvider.StartSetup();
                    VersionedItemTracker versionTracker = new VersionedItemTracker( _startConfiguration.VersionedItemReader );
                    if( versionTracker.Initialize( _monitor ) )
                    {
                        bool setupSuccess = DoRun( items, buildResult.SetupItems, versionTracker, m );
                        setupSuccess &= versionTracker.ConcludeWithFatalOnError( _monitor, _startConfiguration.VersionedItemWriter, setupSuccess );
                        if( setupSuccess )
                        {
                            if( buildResult.GenerateFinalAssemblyIfRequired( _monitor ) )
                            {
                                _startConfiguration.SetupSessionMemoryProvider.StopSetup( null );
                                return true;
                            }
                        }
                    }
                }
                catch( Exception ex )
                {
                    _monitor.Fatal().Send( ex );
                }
                finally
                {
                    _monitor.Output.UnregisterClient( path );
                }
                if( m != null ) memoryProvider.StopSetup( path.LastErrorPath.ToStringPath() );
                return false;
            }
            finally
            {
                DisposeDisposableAspects();
            }
        }

        bool DoRun( object[] items, IEnumerable<ISetupItem> stObjItems, VersionedItemTracker versionTracker, ISetupSessionMemory m )
        {
            bool hasError = false;
            using( _monitor.OnError( () => hasError = true ) )
            using( SetupCoreEngine engine = CreateCoreEngine( versionTracker, m ) )
            {
                using( _monitor.OpenInfo().Send( "Register step." ) )
                {
                    DependencySorterOptions sorterOptions = new DependencySorterOptions() { ReverseName = _config.RunningMode == SetupEngineRunningMode.RevertNames };
                    if( _config.TraceDependencySorterInput ) sorterOptions.HookInput += i => i.Trace( _monitor );
                    if( _config.TraceDependencySorterOutput ) sorterOptions.HookOutput += i => i.Trace( _monitor );
                    sorterOptions.HookInput += _startConfiguration.DependencySorterHookInput;
                    sorterOptions.HookOutput += _startConfiguration.DependencySorterHookOutput;

                    var itemsToRegister = OfTypeRecurse<ISetupItem>( items ).Concat( stObjItems );
                    SetupCoreEngineRegisterResult r = engine.RegisterAndCreateDrivers( itemsToRegister, items.OfType<IDependentItemDiscoverer<ISetupItem>>(), sorterOptions );
                    if( !r.IsValid )
                    {
                        r.LogError( _monitor );
                        return false;
                    }
                    _monitor.CloseGroup( String.Format( "{0} Setup items registered.", r.SortResult.SortedItems.Count ) );
                }
                using( _monitor.OpenInfo().Send( "Init step." ) )
                {
                    if( !engine.RunInit() ) return false;
                }
                using( _monitor.OpenInfo().Send( "Install step." ) )
                {
                    if( !engine.RunInstall() ) return false;
                }
                using( _monitor.OpenInfo().Send( "Settle step." ) )
                {
                    if( !engine.RunSettle() ) return false;
                }
            }
            return !hasError;
        }

        static IEnumerable<T> OfTypeRecurse<T>( IEnumerable e )
        {
            return new Flattennifier().Flatten<T>( e );
        }

        class Flattennifier
        {
            Stack<object> _stack;

            public IEnumerable<T> Flatten<T>( IEnumerable e )
            {
                if( e != null )
                {
                    foreach( object o in e )
                    {
                        if( o is T ) yield return (T)o;
                        // If o is both a T and an IEnumerable, we continue: this
                        // handles composites. For monades, this may lead to a duplicate
                        // (since often the element belongs to its own enumeration).
                        // Such duplicates should not be a surprise for the developper
                        // that works with such funny beast: I prefer to keep handling 
                        // the composition.
                        if( o is IEnumerable && o != e )
                        {
                            if( _stack == null ) _stack = new Stack<object>();
                            else if( _stack.Contains( o ) ) break;
                            _stack.Push( e );
                            foreach( T o2 in Flatten<T>( (IEnumerable)o )) if( o2 != null ) yield return o2;
                            _stack.Pop();
                        }
                    }
                }
            }
        }

        SetupCoreEngine CreateCoreEngine( VersionedItemTracker versionTracker, ISetupSessionMemory m )
        {
            SetupCoreEngine engine = null;
            using( _monitor.OpenInfo().Send( "Setup engine initialization." ) )
            {
                var memory = _startConfiguration.SetupSessionMemoryProvider;
                if( memory.StartCount == 0 ) _monitor.Info().Send( "Starting a new setup." );
                else
                {
                    _monitor.Info().Send( "{0} previous Setup attempt(s). Last on {2}, error was: '{1}'.", memory.StartCount, memory.LastError, memory.LastStartDate );
                }
                engine = new SetupCoreEngine( versionTracker, m, _startConfiguration.Aspects, _monitor, _configurator );
                engine.RegisterSetupEvent += _relayRegisterSetupEvent;
                engine.SetupEvent += _relaySetupEvent;
                engine.DriverEvent += _relayDriverEvent;
            }
            return engine;
        }

        void OnEngineRegisterSetupEvent( object sender, RegisterSetupEventArgs e )
        {
            var h = RegisterSetupEvent;
            if( h != null ) h( this, e );
        }

        void OnEngineSetupEvent( object sender, SetupEventArgs e )
        {
            var h = SetupEvent;
            if( h != null ) h( this, e );
            if( e.Step == SetupStep.Disposed )
            {
                var engine = (SetupCoreEngine)sender;
                engine.RegisterSetupEvent -= _relayRegisterSetupEvent;
                engine.SetupEvent -= _relaySetupEvent;
                engine.DriverEvent -= _relayDriverEvent;
            }
        }

        void OnEngineDriverEvent( object sender, DriverEventArgs e )
        {
            var h = DriverEvent;
            if( h != null ) h( this, e );
        }

    }
}
