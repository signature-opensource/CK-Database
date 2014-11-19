using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Collections;

namespace CK.Setup
{
    public class SetupCenter
    {
        readonly IVersionedItemRepository _versionRepository; 
        readonly ISetupSessionMemoryProvider _memory;
        readonly SetupCenterConfiguration _config;
        readonly SetupableConfigurator _configurator;
        readonly IActivityMonitor _monitor;

        readonly ScriptCollector _scripts;
        readonly ScriptTypeManager _scriptTypeManager;
        readonly EventHandler<RegisterSetupEventArgs> _relayRegisterSetupEvent;
        readonly EventHandler<SetupEventArgs> _relaySetupEvent;
        readonly EventHandler<DriverEventArgs> _relayDriverEvent;

        public SetupCenter( IActivityMonitor monitor, 
                            SetupCenterConfiguration config, 
                            IVersionedItemRepository versionRepository, 
                            ISetupSessionMemoryProvider memory, 
                            IStObjRuntimeBuilder runtimeBuilder = null, 
                            SetupableConfigurator configurator = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "_monitor" );
            if( config == null ) throw new ArgumentNullException( "config" );
            if( versionRepository == null ) throw new ArgumentNullException( "versionRepository" );
            if( memory == null ) throw new ArgumentNullException( "memory" );

            _monitor = monitor;
            _versionRepository = versionRepository;
            _memory = memory;
            _config = config;
            _configurator = new SetupableConfigurator( configurator );

            _scriptTypeManager = new ScriptTypeManager();
            _scripts = new ScriptCollector( _scriptTypeManager );
            _relayRegisterSetupEvent = OnEngineRegisterSetupEvent;
            _relaySetupEvent = OnEngineSetupEvent;
            _relayDriverEvent = OnEngineDriverEvent;

            new StObjSetupHook( this, runtimeBuilder, _config, _configurator );
        }

        /// <summary>
        /// Gets the <see cref="ScriptTypeManager"/> into which <see cref="IScriptTypeHandler"/> must be registered
        /// before <see cref="Run"/> in order for <see cref="ISetupScript"/> added to <see cref="Scripts"/> to be executed.
        /// </summary>
        public ScriptTypeManager ScriptTypeManager
        {
            get { return _scriptTypeManager; }
        }
        
        /// <summary>
        /// Gets the <see cref="ScriptCollector"/>.
        /// </summary>
        public ScriptCollector Scripts
        {
            get { return _scripts; }
        }

        /// <summary>
        /// Gets the _monitor that should be used for the whole setup process.
        /// </summary>
        public IActivityMonitor Logger
        {
            get { return _monitor; }
        }

        /// <summary>
        /// Gets or sets a <see cref="SetupableConfigurator"/> that will be used.
        /// This can be changed at any moment during setup: the current configurator will always be used.
        /// When setting it, care should be taken to not break the chain by setting the current configurator as the <see cref="SetupableConfigurator.Previous"/>.
        /// </summary>
        public SetupableConfigurator SetupableConfigurator
        {
            get { return _configurator.Previous; }
            set { _configurator.Previous = value; }
        }

        /// <summary>
        /// Gets or sets a function that will be called with the list of StObjs once all of them are 
        /// registered in the <see cref="DependencySorter"/> used by the <see cref="StObjCollector"/>.
        /// </summary>
        public Action<IEnumerable<IDependentItem>> StObjDependencySorterHookInput { get; set; }

        /// <summary>
        /// Gets or sets a function that will be called when StObjs have been successfuly sorted by 
        /// the <see cref="DependencySorter"/> used by the <see cref="StObjCollector"/>.
        /// </summary>
        public Action<IEnumerable<ISortedItem>> StObjDependencySorterHookOutput { get; set; }

        /// <summary>
        /// Gets or sets a function that will be called with the list of items once all of them are registered.
        /// This can be used to dump detailed information about items registration and ordering.
        /// </summary>
        public Action<IEnumerable<IDependentItem>> DependencySorterHookInput { get; set; }

        /// <summary>
        /// Gets or sets a function that will be called when items have been sorted.
        /// The final <see cref="DependencySorterResult"/> may not be successful (ie. <see cref="DependencySorterResult.HasStructureError"/> may be true),
        /// but if a cycle has been detected, this hook is not called.
        /// This can be used to dump detailed information about items registration and ordering.
        /// </summary>
        public Action<IEnumerable<ISortedItem>> DependencySorterHookOutput { get; set; }

        /// <summary>
        /// Triggered before registration (at the beginning of <see cref="SetupEgine.Register"/>).
        /// This event fires before the <see cref="SetupEvent"/> (with <see cref="SetupEvent.Step"/> set to None), and enables
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
        /// Executes the whole setup process (<see cref="SetupEngine.Register"/>, <see cref="SetupEngine.RunInit"/>, <see cref="SetupEngine.RunInstall"/>, <see cref="SetupEngine.RunSettle"/>).
        /// </summary>
        /// <param name="items">Objects that can be <see cref="IDependentItem"/>, <see cref="IDependentItemDiscoverer"/> or both.</param>
        /// <returns>True on success, false if an error occured.</returns>
        public bool Run()
        {
            return RunWithExplicitDependentItems();
        }

        /// <summary>
        /// Registers any number of <see cref="IDependentItem"/> and/or <see cref="IDependentItemDiscoverer"/> and/or <see cref="IEnumerable"/> of such objects (recursively) and executes
        /// the whole setup process (<see cref="SetupEngine.Register"/>, <see cref="SetupEngine.RunInit"/>, <see cref="SetupEngine.RunInstall"/>, <see cref="SetupEngine.RunSettle"/>).
        /// </summary>
        /// <param name="items">Objects that can be <see cref="IDependentItem"/>, <see cref="IDependentItemDiscoverer"/> or both and/or <see cref="IEnumerable"/> of such objects (recursively).</param>
        /// <returns>True on success, false if an error occured.</returns>
        public bool RunWithExplicitDependentItems( params object[] items )
        {
            var path = _monitor.Output.RegisterClient( new ActivityMonitorPathCatcher() );
            ISetupSessionMemory m = null;
            try
            {
                m = _memory.StartSetup();
                if( DoRun( items, m ) )
                {
                    _memory.StopSetup( null );
                    return true;
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
            if( m != null ) _memory.StopSetup( path.LastErrorPath.ToStringPath() );
            return false;
        }

        private bool DoRun( object[] items, ISetupSessionMemory m )
        {
            bool hasError = false;
            using( _monitor.CatchCounter( nbError => hasError = true ) )
            using( SetupEngine engine = CreateEngine( m ) )
            {
                using( _monitor.OpenInfo().Send( "Register step." ) )
                {
                    DependencySorter.Options sorterOptions = new DependencySorter.Options() 
                    { 
                        ReverseName = _config.RevertOrderingNames,
                        HookInput = DependencySorterHookInput,
                        HookOutput = DependencySorterHookOutput
                    };
                    SetupEngineRegisterResult r = engine.Register( OfTypeRecurse<IDependentItem>( items ), items.OfType<IDependentItemDiscoverer>(), sorterOptions );
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
            Stack _stack;

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
                            if( _stack == null ) _stack = new Stack();
                            else if( _stack.Contains( o ) ) break;
                            _stack.Push( e );
                            foreach( T o2 in Flatten<T>( (IEnumerable)o )) if( o2 != null ) yield return o2;
                            _stack.Pop();
                        }
                    }
                }
            }
        }

        private SetupEngine CreateEngine( ISetupSessionMemory m )
        {
            SetupEngine engine = null;
            using( _monitor.OpenInfo().Send( "Setup engine initialization." ) )
            {
                if( _memory.StartCount == 0 ) _monitor.Info().Send( "Starting a new setup." );
                else
                {
                    _monitor.Info().Send( "{0} previous Setup attempt(s). Last on {2}, error was: '{1}'.", _memory.StartCount, _memory.LastError, _memory.LastStartDate );
                }
                engine = new SetupEngine( _versionRepository, m, _monitor, _configurator );
                ScriptSetupHandlerBuilder scriptBuilder = new ScriptSetupHandlerBuilder( engine, _scripts, _scriptTypeManager );
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
                var engine = (SetupEngine)sender;
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
