using System;
using System.Collections.Generic;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Collections;
using System.Linq;

namespace CK.Setup
{
    public class SetupableAspect : IStObjEngineAspect
    {
        readonly SetupableAspectConfiguration _config;
        readonly SetupAspectConfigurator _configurator;
        readonly List<object> _externalItems;
        Action<IEnumerable<IDependentItem>> _dependencySorterHookInput;
        Action<IEnumerable<ISortedItem>> _dependencySorterHookOutput;

        IVersionedItemReader _versionedItemReader;
        IVersionedItemWriter _versionedItemWriter;
        ISetupSessionMemoryProvider _setupSessionMemoryProvider;
        ISetupSessionMemory _setupSessionMemory;

        readonly EventHandler<RegisterSetupEventArgs> _relayRegisterSetupEvent;
        readonly EventHandler<SetupEventArgs> _relaySetupEvent;
        readonly EventHandler<DriverEventArgs> _relayDriverEvent;

        class RunConfiguration : ISetupableAspectConfiguration
        {
            readonly SetupableAspect _a;

            public RunConfiguration( SetupableAspect a )
            {
                _a = a;
            }

            public SetupableAspectConfiguration ExternalConfiguration => _a._config;

            public SetupAspectConfigurator Configurator => _a._configurator;

            public IList<object> ExternalItems => _a._externalItems;

            public Action<IEnumerable<IDependentItem>> DependencySorterHookInput
            {
                get => _a._dependencySorterHookInput;
                set => _a._dependencySorterHookInput = value;
            }
            public Action<IEnumerable<ISortedItem>> DependencySorterHookOutput
            {
                get => _a._dependencySorterHookOutput;
                set => _a._dependencySorterHookOutput = value;
            }
        }

        public SetupableAspect( SetupableAspectConfiguration config )
        {
            _config = config;
            _configurator = new SetupAspectConfigurator();
            _externalItems = new List<object>();
            _relayRegisterSetupEvent = OnEngineRegisterSetupEvent;
            _relaySetupEvent = OnEngineSetupEvent;
            _relayDriverEvent = OnEngineDriverEvent;
        }

        public bool Configure( IActivityMonitor monitor, IStObjEngineConfigureContext context )
        {

            context.PushPostConfigureAction( PostConfigure );
            return true;
        }

        bool PostConfigure( IActivityMonitor monitor, IStObjEngineConfigureContext context )
        {
            _versionedItemReader = context.ServiceContainer.GetService<IVersionedItemReader>( true );
            _versionedItemWriter = context.ServiceContainer.GetService<IVersionedItemWriter>( true );
            _setupSessionMemoryProvider = context.ServiceContainer.GetService<ISetupSessionMemoryProvider>( true );
            return true;
        }

        public event EventHandler<RegisterSetupEventArgs> RegisterSetupEvent;

        public event EventHandler<SetupEventArgs> SetupEvent;

        public event EventHandler<DriverEventArgs> DriverEvent;

        public bool Run( IActivityMonitor monitor, IStObjEngineRunContext context )
        {
            var configurator = _configurator.FirstLayer;
            var itemBuilder = new StObjSetupItemBuilder( monitor, context.Aspects, configurator, configurator, configurator );
            IEnumerable<ISetupItem> setupItems = itemBuilder.Build( context.OrderedStObjs );
            if( setupItems == null ) return false;

            _setupSessionMemory = _setupSessionMemoryProvider.StartSetup();
            VersionedItemTracker versionTracker = new VersionedItemTracker( _versionedItemReader );
            if( versionTracker.Initialize( monitor ) )
            {
                bool setupSuccess = DoRun( monitor, context.Aspects, setupItems, versionTracker, _setupSessionMemory );
                setupSuccess &= versionTracker.ConcludeWithFatalOnError( monitor, _versionedItemWriter, setupSuccess );
                return setupSuccess;
            }
            return false;
        }

        public bool Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context )
        {
            if( context.EngineStatus.Success )
            {
                _setupSessionMemoryProvider.StopSetup( null );
            }
            else
            {
                _setupSessionMemoryProvider.StopSetup( context.EngineStatus.LastErrorPath.ToStringPath() );
            }
            return true;
        }

        bool DoRun( IActivityMonitor monitor, IReadOnlyList<IStObjEngineAspect> aspects, IEnumerable<ISetupItem> stObjItems, VersionedItemTracker versionTracker, ISetupSessionMemory m )
        {
            bool hasError = false;
            using( monitor.OnError( () => hasError = true ) )
            using( SetupCoreEngine engine = CreateCoreEngine( monitor, aspects, versionTracker, m ) )
            {
                using( monitor.OpenInfo( "Register step." ) )
                {
                    DependencySorterOptions sorterOptions = new DependencySorterOptions() { ReverseName = _config.RevertOrderingNames };
                    if( _config.TraceDependencySorterInput ) sorterOptions.HookInput += i => i.Trace( monitor );
                    if( _config.TraceDependencySorterOutput ) sorterOptions.HookOutput += i => i.Trace( monitor );
                    sorterOptions.HookInput += _dependencySorterHookInput;
                    sorterOptions.HookOutput += _dependencySorterHookOutput;

                    var itemsToRegister = OfTypeRecurse<ISetupItem>( _externalItems ).Concat( stObjItems );
                    SetupCoreEngineRegisterResult r = engine.RegisterAndCreateDrivers( itemsToRegister, _externalItems.OfType<IDependentItemDiscoverer<ISetupItem>>(), sorterOptions );
                    if( !r.IsValid )
                    {
                        r.LogError( monitor );
                        return false;
                    }
                    monitor.CloseGroup( $"{r.SortResult.SortedItems.Count} Setup items registered." );
                }
                using( monitor.OpenInfo( "Init step." ) )
                {
                    if( !engine.RunInit() ) return false;
                }
                using( monitor.OpenInfo( "Install step." ) )
                {
                    if( !engine.RunInstall() ) return false;
                }
                using( monitor.OpenInfo( "Settle step." ) )
                {
                    if( !engine.RunSettle() ) return false;
                }
            }
            return !hasError;
        }

        static IEnumerable<T> OfTypeRecurse<T>( IEnumerable e ) => new Flattennifier().Flatten<T>( e );

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
                        // handles composites. For "monades", this may lead to a duplicate
                        // (since often the element belongs to its own enumeration).
                        // Such duplicates should not be a surprise for the developper
                        // that works with such funny beast: I prefer to keep handling 
                        // the composition.
                        if( o is IEnumerable && o != e )
                        {
                            if( _stack == null ) _stack = new Stack<object>();
                            else if( _stack.Contains( o ) ) break;
                            _stack.Push( e );
                            foreach( T o2 in Flatten<T>( (IEnumerable)o ) ) if( o2 != null ) yield return o2;
                            _stack.Pop();
                        }
                    }
                }
            }
        }

        SetupCoreEngine CreateCoreEngine( IActivityMonitor monitor, IReadOnlyList<IStObjEngineAspect> aspects, VersionedItemTracker versionTracker, ISetupSessionMemory m )
        {
            SetupCoreEngine engine = null;
            using( monitor.OpenInfo( "Setupable Core Engine initialization." ) )
            {
                var memory = _setupSessionMemoryProvider;
                if( memory.StartCount == 0 ) monitor.Info( "Starting a new setup." );
                else
                {
                    monitor.Info( $"{memory.StartCount} previous Setup attempt(s). Last on {memory.LastStartDate}, error was: '{memory.LastError}'." );
                }
                engine = new SetupCoreEngine( versionTracker, m, aspects, monitor, _configurator.FirstLayer );
                engine.RegisterSetupEvent += _relayRegisterSetupEvent;
                engine.SetupEvent += _relaySetupEvent;
                engine.DriverEvent += _relayDriverEvent;
            }
            return engine;
        }

        void OnEngineRegisterSetupEvent( object sender, RegisterSetupEventArgs e ) => RegisterSetupEvent?.Invoke( this, e );

        void OnEngineSetupEvent( object sender, SetupEventArgs e )
        {
            SetupEvent?.Invoke( this, e );
            if( e.Step == SetupStep.Disposed )
            {
                var engine = (SetupCoreEngine)sender;
                engine.RegisterSetupEvent -= _relayRegisterSetupEvent;
                engine.SetupEvent -= _relaySetupEvent;
                engine.DriverEvent -= _relayDriverEvent;
            }
        }

        void OnEngineDriverEvent( object sender, DriverEventArgs e ) => DriverEvent?.Invoke( this, e );

    }
}
