using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public class SetupCenter
    {
        IVersionedItemRepository _versionRepository; 
        ISetupSessionMemoryProvider _memory;
        IActivityLogger _logger;
        ISetupDriverFactory _driverFactory;
        
        PackageScriptCollector _scripts;
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
            _scripts = new PackageScriptCollector( _scriptTypeManager.IsRegistered );
        }

        public ScriptTypeManager ScriptTypeManager
        {
            get { return _scriptTypeManager; }
        }
        
        public PackageScriptCollector Scripts
        {
            get { return _scripts; }
        }

        /// <summary>
        /// Gets ors sets whether the ordering for setupable items that share the same rank in the pure dependency graph must be inverted.
        /// Defaults to true.
        /// (see <see cref="DependencySorter"/> for more information.)
        /// </summary>
        public bool RevertOrderingNames { get; set; }

        /// <summary>
        /// Registers any number of <see cref="ISetupableItem"/> and/or <see cref="IDependentItemDiscoverer"/> and executes
        /// the whole setup process (<see cref="SetupEngine.RunInit"/>, <see cref="SetupEngine.RunInit"/>, <see cref="SetupEngine.RunInstall"/>, <see cref="SetupEngine.RunSettle"/>).
        /// </summary>
        /// <param name="items">Objects that can be <see cref="ISetupableItem"/>, <see cref="IDependentItemDiscoverer"/> or both.</param>
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
                _logger.Output.UnregisterMuxClient( path );
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
                    SetupEngineRegisterResult r = engine.Register( items.OfType<ISetupableItem>(), items.OfType<IDependentItemDiscoverer>(), RevertOrderingNames );
                    if( !r.IsValid )
                    {
                        r.LogError( _logger );
                        return false;
                    }
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
                ScriptHandlerBuilder scriptBuilder = new ScriptHandlerBuilder( engine, _scripts, _scriptTypeManager );
            }
            return engine;
        }

    }
}
