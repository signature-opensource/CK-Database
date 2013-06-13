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
        IVersionedItemRepository _versionRepository; 
        ISetupSessionMemoryProvider _memory;
        IActivityLogger _logger;
        ISetupDriverFactory _driverFactory;
        
        ScriptCollector _scripts;
        ScriptTypeManager _scriptTypeManager;

        public SetupCenter( IVersionedItemRepository versionRepository, ISetupSessionMemoryProvider memory, IActivityLogger logger, ISetupDriverFactory driverFactory )
        {
            if( versionRepository == null ) throw new ArgumentNullException( "versionRepository" );
            if( memory == null ) throw new ArgumentNullException( "memory" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( driverFactory == null ) throw new ArgumentNullException( "driverFactory" );

            _versionRepository = versionRepository;
            _memory = memory;
            _logger = logger;
            _driverFactory = driverFactory;

            _scriptTypeManager = new ScriptTypeManager();
            _scripts = new ScriptCollector( _scriptTypeManager );
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
        /// Gets or sets a function that will be called with the list of items once all of them are registered.
        /// </summary>
        public Action<IEnumerable<IDependentItem>> DependencySorterHookInput { get; set; }

        /// <summary>
        /// Gets or sets a function that will be called when items have been sorted.
        /// The final <see cref="DependencySorterResult"/> may not be successful (ie. <see cref="DependencySorterResult.HasStructureError"/> may be true),
        /// but if a cycle has been detected, this hook is not called.
        /// </summary>
        public Action<IEnumerable<ISortedItem>> DependencySorterHookOutput { get; set; }

        /// <summary>
        /// Gets ors sets whether the ordering for setupable items that share the same rank in the pure dependency graph must be inverted.
        /// Defaults to false. (See <see cref="DependencySorter"/> for more information.)
        /// </summary>
        public bool RevertOrderingNames { get; set; }

        /// <summary>
        /// Registers any number of <see cref="IDependentItem"/> and/or <see cref="IDependentItemDiscoverer"/> and executes
        /// the whole setup process (<see cref="SetupEngine.RunInit"/>, <see cref="SetupEngine.RunInit"/>, <see cref="SetupEngine.RunInstall"/>, <see cref="SetupEngine.RunSettle"/>).
        /// </summary>
        /// <param name="items">Objects that can be <see cref="IDependentItem"/>, <see cref="IDependentItemDiscoverer"/> or both.</param>
        /// <returns>A <see cref="SetupEngineRegisterResult"/> that captures detailed information about the registration result.</returns>
        public bool Run( params object[] items )
        {
            ActivityLoggerPathCatcher path = new ActivityLoggerPathCatcher();
            _logger.Output.RegisterClient( path );
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
                _logger.Fatal( ex );
            }
            finally
            {
                _logger.Output.UnregisterClient( path );
            }
            if( m != null ) _memory.StopSetup( path.LastErrorPath.ToStringPath() );
            return false;
        }

        private bool DoRun( object[] items, ISetupSessionMemory m )
        {
            using( SetupEngine engine = CreateEngine( m ) )
            {
                using( _logger.OpenGroup( LogLevel.Info, "Register step." ) )
                {
                    DependencySorter.Options sorterOptions = new DependencySorter.Options() 
                    { 
                        ReverseName = RevertOrderingNames,
                        HookInput = DependencySorterHookInput,
                        HookOutput = DependencySorterHookOutput
                    };
                    SetupEngineRegisterResult r = engine.Register( OfTypeRecurse<IDependentItem>( items ), items.OfType<IDependentItemDiscoverer>(), sorterOptions );
                    if( !r.IsValid )
                    {
                        r.LogError( _logger );
                        return false;
                    }
                    _logger.CloseGroup( String.Format( "{0} Setup items registered.", r.SortResult.SortedItems.Count ) );
                }
                using( _logger.OpenGroup( LogLevel.Info, "Init step." ) )
                {
                    if( !engine.RunInit() ) return false;
                }
                using( _logger.OpenGroup( LogLevel.Info, "Run step." ) )
                {
                    if( !engine.RunInstall() ) return false;
                }
                using( _logger.OpenGroup( LogLevel.Info, "Settle step." ) )
                {
                    if( !engine.RunSettle() ) return false;
                }
            }
            return true;
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
            using( _logger.OpenGroup( LogLevel.Info, "Setup engine initialization." ) )
            {
                if( _memory.StartCount == 0 ) _logger.Info( "Starting a new setup." );
                else
                {
                    _logger.Info( "{0} previous Setup attempt(s). Last on {2}, error was: '{1}'.", _memory.StartCount, _memory.LastError, _memory.LastStartDate );
                }
                engine = new SetupEngine( _versionRepository, m, _logger, _driverFactory );
                ScriptSetupHandlerBuilder scriptBuilder = new ScriptSetupHandlerBuilder( engine, _scripts, _scriptTypeManager );
            }
            return engine;
        }

    }
}
