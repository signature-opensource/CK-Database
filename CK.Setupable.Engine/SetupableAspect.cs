using System;
using System.Collections.Generic;
using CK.Core;
using System.Diagnostics;
using System.Collections;
using System.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Implements <see cref="ISetupableAspect"/>.
    /// </summary>
    public class SetupableAspect : IStObjEngineAspect, ISetupableAspect
    {
        readonly SetupableAspectConfiguration _config;
        readonly SetupAspectConfigurator _configurator;
        readonly List<object> _externalItems;

        IVersionedItemReader _versionedItemReader;
        IVersionedItemWriter _versionedItemWriter;
        ISetupSessionMemoryProvider _setupSessionMemoryProvider;
        ISetupSessionMemory _setupSessionMemory;

        readonly EventHandler<RegisterSetupEventArgs> _relayRegisterSetupEvent;
        readonly EventHandler<SetupEventArgs> _relaySetupEvent;
        readonly EventHandler<DriverEventArgs> _relayDriverEvent;

        class RunConfiguration : ISetupableAspectRunConfiguration
        {
            readonly SetupableAspect _a;

            public RunConfiguration( SetupableAspect a )
            {
                _a = a;
            }

            public SetupableAspectConfiguration ExternalConfiguration => _a._config;

            public SetupAspectConfigurator Configurator => _a._configurator;

            public IList<object> ExternalItems => _a._externalItems;
        }

        /// <summary>
        /// Initializes a new <see cref="SetupableAspect"/>.
        /// </summary>
        /// <param name="config">The aspect configuration.</param>
        public SetupableAspect( SetupableAspectConfiguration config )
        {
            _config = config;
            _configurator = new SetupAspectConfigurator();
            _externalItems = new List<object>();
            _relayRegisterSetupEvent = OnEngineRegisterSetupEvent;
            _relaySetupEvent = OnEngineSetupEvent;
            _relayDriverEvent = OnEngineDriverEvent;
        }

        bool IStObjEngineAspect.Configure( IActivityMonitor monitor, IStObjEngineConfigureContext context )
        {
            context.AddConfigureOnlyService( new ConfigureOnly<ISetupableAspectRunConfiguration>( new RunConfiguration( this ) ) );
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

        /// <summary>
        /// This event fires before the <see cref="SetupEvent"/> (with <see cref="SetupEventArgs.Step"/> set to None), and enables
        /// registration of setup items.
        /// </summary>
        public event EventHandler<RegisterSetupEventArgs> RegisterSetupEvent;

        /// <summary>
        /// Triggered for each steps of <see cref="SetupStep"/>: None (before registration), Init, Install, Settle and Done.
        /// </summary>
        public event EventHandler<SetupEventArgs> SetupEvent;

        /// <summary>
        /// Triggered for each <see cref="DriverBase"/> setup phasis.
        /// </summary>
        public event EventHandler<DriverEventArgs> DriverEvent;

        bool IStObjEngineAspect.Run( IActivityMonitor monitor, IStObjEngineRunContext context )
        {
            var configurator = _configurator.FirstLayer;
            var itemBuilder = new StObjSetupItemBuilder( monitor, context.ServiceContainer, configurator, configurator, configurator );
            IEnumerable<ISetupItem> setupItems = itemBuilder.Build( context.OrderedStObjs );
            if( setupItems == null ) return false;

            _setupSessionMemory = _setupSessionMemoryProvider.StartSetup();
            VersionedItemTracker versionTracker = new VersionedItemTracker( _versionedItemReader );
            if( versionTracker.Initialize( monitor ) )
            {
                context.ServiceContainer.Add( _setupSessionMemory );
                bool setupSuccess = DoRun( monitor, context.ServiceContainer, setupItems, versionTracker );
                setupSuccess &= versionTracker.Conclude( monitor, _versionedItemWriter, setupSuccess && !_config.KeepUnaccessedItemsVersion, context.Features );
                return setupSuccess;
            }
            return false;
        }

        bool IStObjEngineAspect.Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context )
        {
            if( context.EngineStatus.Success )
            {
                Debug.Assert( _setupSessionMemory != null );
                _setupSessionMemoryProvider.StopSetup( null );
                context.ServiceContainer.Remove<ISetupSessionMemory>();
            }
            else
            {
                if( _setupSessionMemory != null )
                {
                    _setupSessionMemoryProvider.StopSetup( context.EngineStatus.LastErrorPath.ToStringPath() );
                    context.ServiceContainer.Remove<ISetupSessionMemory>();
                }
            }
            return true;
        }

        bool DoRun( IActivityMonitor monitor, IServiceProvider services, IEnumerable<ISetupItem> stObjItems, VersionedItemTracker versionTracker )
        {
            bool hasError = false;
            using( monitor.OnError( () => hasError = true ) )
            using( SetupCoreEngine engine = CreateCoreEngine( monitor, services, versionTracker ) )
            {
                using( monitor.OpenInfo( "Register step." ) )
                {
                    DependencySorterOptions sorterOptions = new DependencySorterOptions() { ReverseName = _config.RevertOrderingNames };
                    if( _config.TraceDependencySorterInput ) sorterOptions.HookInput += i => i.Trace( monitor );
                    if( _config.TraceDependencySorterOutput ) sorterOptions.HookOutput += i => i.Trace( monitor );

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

        SetupCoreEngine CreateCoreEngine( IActivityMonitor monitor, IServiceProvider services, VersionedItemTracker versionTracker )
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
                engine = new SetupCoreEngine( versionTracker, services, monitor, _configurator.FirstLayer );
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
