using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Core setup object. Contains the execution context and all ambient services required to
    /// process a setup.
    /// It does not contain anything related to script management: the <see cref="SetupCenter"/> wraps
    /// this class and offers package script support (see <see cref="SetupCenter.ScriptTypeManager"/> and <see cref="SetupCenter.Scripts"/>).
    /// </summary>
    public sealed class SetupEngine : IDisposable
    {
        readonly IVersionedItemRepository _versionRepository;
        readonly DriverList _drivers;
        readonly ISetupDriverFactory _driverFactory;
        readonly IActivityLogger _logger;
        readonly ISetupSessionMemory _memory;
        SetupEngineState _state;

        class DriverList : ISetupDriverList
        {
            Dictionary<string,SetupDriverBase> _byName;
            List<SetupDriverBase> _drivers;
            SetupEngine _center;

            public DriverList( SetupEngine center )
            {
                _center = center;
                _byName = new Dictionary<string, SetupDriverBase>();
                _drivers = new List<SetupDriverBase>();
            }

            public SetupDriverBase this[string fullName]
            {
                get { return _byName.GetValueWithDefault( fullName, null ); }
            }

            public int IndexOf( object item )
            {
                SetupDriverBase d = item as SetupDriverBase;
                return d != null ? d.Index : -1;
            }

            public SetupDriverBase this[int index]
            {
                get { return _drivers[index]; }
            }

            public bool Contains( object item )
            {
                SetupDriverBase d = item as SetupDriverBase;
                return d != null ? d.Engine == _center : false;
            }

            public int Count
            {
                get { return _byName.Count; }
            }

            public IEnumerator<SetupDriverBase> GetEnumerator()
            {
                return _drivers.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _drivers.GetEnumerator();
            }

            internal void Clear()
            {
                _byName.Clear();
                _drivers.Clear();
            }

            internal void Add( SetupDriverBase d )
            {
                Debug.Assert( d != null && d.Engine == _center );
                Debug.Assert( !_byName.ContainsKey( d.FullName ) );
                Debug.Assert( _drivers.Count == 0 || _drivers[_drivers.Count-1].Index < d.Index );
                _drivers.Add( d );
                _byName.Add( d.FullName, d );
            }

        }

        class DefaultDriverfactory : ISetupDriverFactory
        {
            public readonly static ISetupDriverFactory Default = new DefaultDriverfactory();

            SetupDriver ISetupDriverFactory.CreateDriver( Type driverType, SetupDriver.BuildInfo info )
            {
                return (SetupDriver)Activator.CreateInstance( driverType, info );
            }

            SetupDriverContainer ISetupDriverFactory.CreateDriverContainer( Type containerType, SetupDriverContainer.BuildInfo info )
            {
                return (SetupDriverContainer)Activator.CreateInstance( containerType, info );
            }

        }

        public SetupEngine( IVersionedItemRepository versionRepository, ISetupSessionMemory memory, IActivityLogger logger, ISetupDriverFactory driverFactory )
        {
            if( versionRepository == null ) throw new ArgumentNullException( "versionRepository" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( memory == null ) throw new ArgumentNullException( "memory" );
            _versionRepository = versionRepository;
            _memory = memory;
            _driverFactory = driverFactory ?? DefaultDriverfactory.Default;
            _logger = logger;
            _drivers = new DriverList( this );
        }

        public event EventHandler<SetupDriverEventArgs> DriverEvent;

        public IVersionedItemRepository VersionRepository
        {
            get { return _versionRepository; }
        }

        public ISetupSessionMemory Memory
        {
            get { return _memory; }
        }

        public IActivityLogger Logger
        {
            get { return _logger; }
        }

        public ISetupDriverList AllDrivers
        {
            get { return _drivers; }
        }

        public bool RegisterDone
        {
            get { return _drivers.Count > 0; }
        }

        public SetupEngineState State
        {
            get { return _state; }
        }

        /// <summary>
        /// Registers any number of <see cref="ISetupableItem"/> and/or <see cref="IDependentItemDiscoverer"/> and optionnaly registers them
        /// with an inverted setup order between independant items (see <see cref="DependencySorter"/> for more information).
        /// </summary>
        /// <param name="items">Set of <see cref="ISetupableItem"/></param>
        /// <param name="discoverers">Set of <see cref="IDependentItemDiscoverer"/>.</param>
        /// <returns>A <see cref="SetupEngineRegisterResult"/> that captures detailed information about the registration result.</returns>
        /// <param name="reverseName">Reverse the ordering for items that share the same rank in the pure dependency graph.</param>
        public SetupEngineRegisterResult Register( IEnumerable<ISetupableItem> items, IEnumerable<IDependentItemDiscoverer> discoverers, bool reverseName = false )
        {
            CheckState( SetupEngineState.None );
            SetupEngineRegisterResult result = null;
            try
            {
                result = new SetupEngineRegisterResult( DependencySorter.OrderItems( items, discoverers, reverseName ) );
                if( result.IsValid )
                {
                    var reusableEvent = new SetupDriverEventArgs( SetupStep.None );
                    foreach( var item in result.SortResult.SortedItems )
                    {
                        SetupDriverBase d;
                        Type typeToCreate = null;
                        if( item.IsContainer )
                        {
                            var head = _drivers[item.HeadForContainer.FullName] as SetupDriverHead;
                            Debug.Assert( head != null );
                            typeToCreate = ResolveDriverType( item );
                            SetupDriverContainer c = _driverFactory.CreateDriverContainer( typeToCreate, new SetupDriverContainer.BuildInfo( head, item ) );
                            d = head.Container = c;
                        }
                        else
                        {
                            VersionedName externalVersion = VersionRepository.GetCurrent( (ISetupableItem)item.Item );
                            if( item.IsContainerHead )
                            {
                                d = new SetupDriverHead( this, item, externalVersion );
                            }
                            else
                            {
                                typeToCreate = ResolveDriverType( item );
                                d = _driverFactory.CreateDriver( typeToCreate, new SetupDriver.BuildInfo( this, item, externalVersion ) );
                            }
                        }
                        if( d == null ) throw new Exception( String.Format( "Driver Factory returned null for item {0}, type {1}", item.FullName, typeToCreate ) );
                        _drivers.Add( d );
                        var hE = DriverEvent;
                        if( hE != null )
                        {
                            reusableEvent.Driver = d;
                            hE( this, reusableEvent );
                            if( reusableEvent.CancelSetup )
                            {
                                result.CanceledRegistrationCulprit = item;
                                _drivers.Clear();
                                break;
                            }
                        }
                    }
                    if( result.CanceledRegistrationCulprit == null ) _state = SetupEngineState.Registered;
                }
            }
            catch( Exception ex )
            {
                if( result == null ) result = new SetupEngineRegisterResult( null );
                _drivers.Clear();
                result.UnexpectedError = ex;
            }
            return result;
        }

        public bool RunInit()
        {
            CheckState( SetupEngineState.Registered );
            _state = SetupEngineState.InitializationError;
            try
            {
                var reusableEvent = new SetupDriverEventArgs( SetupStep.Init );
                foreach( var d in _drivers )
                {
                    using( _logger.OpenGroup( LogLevel.Info, "Initializing {0}", d.FullName ) )
                    {
                        if( !d.ExecuteInit() ) return false;
                        var hE = DriverEvent;
                        if( hE != null )
                        {
                            reusableEvent.Driver = d;
                            hE( this, reusableEvent );
                            if( reusableEvent.CancelSetup ) return false;
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                _logger.Fatal( ex );
                return false;
            }
            _state = SetupEngineState.Initialized;
            return true;
        }

        public bool RunInstall()
        {
            CheckState( SetupEngineState.Initialized );
            _state = SetupEngineState.InstallationError;
            try
            {
                var reusableEvent = new SetupDriverEventArgs( SetupStep.Install );
                foreach( var d in _drivers )
                {
                    using( _logger.OpenGroup( LogLevel.Info, "Installing {0}", d.FullName ) )
                    {
                        if( !d.ExecuteInstall() ) return false;
                        var hE = DriverEvent;
                        if( hE != null )
                        {
                            reusableEvent.Driver = d;
                            hE( this, reusableEvent );
                            if( reusableEvent.CancelSetup ) return false;
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                _logger.Fatal( ex );
                return false;
            }
            _state = SetupEngineState.Installed;
            return true;
        }

        public bool RunSettle()
        {
            CheckState( SetupEngineState.Installed );
            _state = SetupEngineState.SettlementError;
            try
            {
                var reusableEvent = new SetupDriverEventArgs( SetupStep.Settle );
                foreach( var d in _drivers )
                {
                    using( _logger.OpenGroup( LogLevel.Info, "Settling {0}", d.FullName ) )
                    {
                        if( !d.ExecuteSettle() ) return false;
                        var hE = DriverEvent;
                        if( hE != null )
                        {
                            reusableEvent.Driver = d;
                            hE( this, reusableEvent );
                            if( reusableEvent.CancelSetup ) return false;
                        }
                        VersionRepository.SetCurrent( d.Item );

                    }
                }
            }
            catch( Exception ex )
            {
                _logger.Fatal( ex );
                return false;
            }
            _state = SetupEngineState.Settled;
            return true;
        }

        public void Dispose()
        {
            if( (_state & SetupEngineState.Disposed) == 0 )
            {
                using( Logger.OpenGroup( LogLevel.Info, "Disposing {0} drivers.", _drivers.Count ) )
                {
                    foreach( var d in _drivers )
                    {
                        IDisposable id = d as IDisposable;
                        if( id != null )
                        {
                            try
                            {
                                id.Dispose();
                            }
                            catch( Exception ex )
                            {
                                Logger.Error( ex, "Disposing {0} of type '{1}'.", d.FullName, d.GetType() );
                            }
                        }
                    }
                }
                _state |= SetupEngineState.Disposed;
            }
        }

        private static Type ResolveDriverType( ISortedItem item )
        {
            string typeName = ((ISetupableItem)item.Item).SetupDriverTypeName;
            Type t = SimpleTypeFinder.Default.ResolveType( typeName, true );
            return t;
        }

        void CheckState( SetupEngineState requiredState )
        {
            if( _state != requiredState )
            {
                throw new InvalidOperationException( String.Format( "Invalid SetupCenter state: {0} expected but was {1}", requiredState, _state.ToString() ) );
            }
        }

    }
}
