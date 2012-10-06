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

        class DriverList : IDriverList
        {
            Dictionary<object,DriverBase> _index;
            List<DriverBase> _drivers;
            SetupEngine _center;

            public DriverList( SetupEngine center )
            {
                _center = center;
                _index = new Dictionary<object, DriverBase>();
                _drivers = new List<DriverBase>();
            }

            public DriverBase this[string fullName]
            {
                get { return _index.GetValueWithDefault( fullName, null ); }
            }

            public DriverBase this[ IDependentItem item ]
            {
                get { return _index.GetValueWithDefault( item, null ); }
            }

            public int IndexOf( object driver )
            {
                DriverBase d = driver as DriverBase;
                return d != null && d.Engine == _center ? d.Index : -1;
            }

            public DriverBase this[int index]
            {
                get { return _drivers[index]; }
            }

            public bool Contains( object driver )
            {
                DriverBase d = driver as DriverBase;
                return d != null ? d.Engine == _center : false;
            }

            public int Count
            {
                get { return _drivers.Count; }
            }

            public IEnumerator<DriverBase> GetEnumerator()
            {
                return _drivers.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _drivers.GetEnumerator();
            }

            internal void Clear()
            {
                _index.Clear();
                _drivers.Clear();
            }

            internal void Add( DriverBase d )
            {
                Debug.Assert( d != null && d.Engine == _center );
                Debug.Assert( !_index.ContainsKey( d.FullName ) );
                Debug.Assert( _drivers.Count == 0 || _drivers[_drivers.Count-1].Index < d.Index );
                _drivers.Add( d );
                _index.Add( d.FullName, d );
                if( !d.IsGroupHead ) _index.Add( d.Item, d );
            }

        }

        class DefaultDriverfactory : ISetupDriverFactory
        {
            public readonly static ISetupDriverFactory Default = new DefaultDriverfactory();

            SetupDriver ISetupDriverFactory.CreateDriver( Type containerType, SetupDriver.BuildInfo info )
            {
                return (SetupDriver)Activator.CreateInstance( containerType, info );
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

        /// <summary>
        /// Triggered for each steps of <see cref="SetupStep"/>: None (before registration), Init, Install, Settle and Done.
        /// </summary>
        public event EventHandler<SetupEventArgs> SetupEvent;

        /// <summary>
        /// Triggered for each <see cref="DriverBase"/> setup phasis.
        /// </summary>
        public event EventHandler<DriverEventArgs> DriverEvent;
        
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

        public IDriverList AllDrivers
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
        /// Registers any number of <see cref="IDependentItem"/> and/or <see cref="IDependentItemDiscoverer"/> and optionnaly registers them
        /// with an inverted setup order between independant items (see <see cref="DependencySorter"/> for more information).
        /// </summary>
        /// <param name="items">Set of <see cref="IDependentItem"/></param>
        /// <param name="discoverers">Set of <see cref="IDependentItemDiscoverer"/>.</param>
        /// <returns>A <see cref="SetupEngineRegisterResult"/> that captures detailed information about the registration result.</returns>
        /// <param name="options">Optional configuration for dependecy graph computation.</param>
        public SetupEngineRegisterResult Register( IEnumerable<IDependentItem> items, IEnumerable<IDependentItemDiscoverer> discoverers, DependencySorter.Options options = null )
        {
            CheckState( SetupEngineState.None );
            
            // Because of the SetupEngineRegisterResult encapsulation for this Register phasis, it is not easy to reuse FireSetupEvent.
            if( SetupEvent != null )
            {
                var e = new SetupEventArgs( SetupStep.None );
                try
                {
                    SetupEvent( this, e );
                    if( e.CancelReason != null )
                    {
                        return new SetupEngineRegisterResult( null ) { CancelReason = e.CancelReason };
                    }
                }
                catch( Exception ex )
                {
                    return new SetupEngineRegisterResult( null ) { UnexpectedError = ex };
                }
            }

            // There is no _state = SetupEngineState.RegistrationError since on error we clear the driver list and
            // the state remains set to SetupEngineState.None.
            SetupEngineRegisterResult result = null;
            try
            {
                result = new SetupEngineRegisterResult( DependencySorter.OrderItems( items, discoverers, options ) );
                if( result.IsValid )
                {
                    var reusableEvent = new DriverEventArgs( SetupStep.None );
                    foreach( var item in result.SortResult.SortedItems )
                    {
                        DriverBase d;
                        Type typeToCreate = null;
                        if( item.IsGroup )
                        {
                            var head = (GroupHeadSetupDriver)_drivers[item.HeadForGroup.FullName];
                            typeToCreate = ResolveDriverType( item );
                            SetupDriver c = _driverFactory.CreateDriver( typeToCreate, new SetupDriver.BuildInfo( head, item ) );
                            d = head.Container = c;
                        }
                        else
                        {
                            VersionedName externalVersion;
                            IVersionedItem versioned = item.Item as IVersionedItem;
                            if( versioned != null ) externalVersion = VersionRepository.GetCurrent( versioned );
                            else externalVersion = null;

                            if( item.IsGroupHead )
                            {
                                d = new GroupHeadSetupDriver( this, item, externalVersion );
                            }
                            else
                            {
                                typeToCreate = ResolveDriverType( item );
                                d = _driverFactory.CreateDriver( typeToCreate, new SetupDriver.BuildInfo( this, item, externalVersion ) );
                            }
                        }
                        if( d == null ) throw new Exception( String.Format( "Driver Factory returned null for item {0}, type '{1}'.", item.FullName, typeToCreate ) );
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
                }
            }
            catch( Exception ex )
            {
                // Exception is not logged at this level: it is carried by the SetupEngineRegisterResult
                // and its LogError method must be used to log different kind of errors.
                if( result == null ) result = new SetupEngineRegisterResult( null );
                result.UnexpectedError = ex;
                _drivers.Clear();
            }
            if( result.IsValid ) _state = SetupEngineState.Registered;
            else 
            {
                SafeFireSetupEvent( SetupStep.None, true );
            }
            return result;
        }

        private bool SafeFireSetupEvent( SetupStep step, bool errorOccured = false )
        {
            if( SetupEvent == null ) return true;
            using( _logger.OpenGroup( LogLevel.Trace, errorOccured ? "Raising error event during {0}." : "Raising {0} setup event.", step ) )
            {
                var e = new SetupEventArgs( step );
                try
                {
                    SetupEvent( this, e );
                    if( e.CancelReason == null ) return true;
                    _logger.Fatal( e.CancelReason );
                }
                catch( Exception ex )
                {
                    _logger.Fatal( ex );
                }
            }
            return false;
        }

        public bool RunInit()
        {
            CheckState( SetupEngineState.Registered );
            _state = SetupEngineState.InitializationError;
            if( !SafeFireSetupEvent( SetupStep.Init ) ) return false;
            try
            {
                var reusableEvent = new DriverEventArgs( SetupStep.Init );
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
                SafeFireSetupEvent( SetupStep.Init, true );
                return false;
            }
            _state = SetupEngineState.Initialized;
            return true;
        }

        public bool RunInstall()
        {
            CheckState( SetupEngineState.Initialized );
            _state = SetupEngineState.InstallationError;
            if( !SafeFireSetupEvent( SetupStep.Install ) ) return false;
            try
            {
                var reusableEvent = new DriverEventArgs( SetupStep.Install );
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
                SafeFireSetupEvent( SetupStep.Install, true );
                return false;
            }
            _state = SetupEngineState.Installed;
            return true;
        }

        public bool RunSettle()
        {
            CheckState( SetupEngineState.Installed );
            _state = SetupEngineState.SettlementError;
            if( !SafeFireSetupEvent( SetupStep.Settle ) ) return false;
            try
            {
                var reusableEvent = new DriverEventArgs( SetupStep.Settle );
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
                        IVersionedItem versioned = d.Item as IVersionedItem;
                        if( versioned != null ) VersionRepository.SetCurrent( versioned );
                    }
                }
            }
            catch( Exception ex )
            {
                _logger.Fatal( ex );
                SafeFireSetupEvent( SetupStep.Settle, true );
                return false;
            }
            _state = SetupEngineState.Settled;
            SafeFireSetupEvent( SetupStep.Done );
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
            if( item.StartValue is Type ) return (Type)item.StartValue;
            if( !(item.StartValue is string) )
            {
                if( item.StartValue == null )
                {
                    throw new CKException( "StartDependencySort returned a null object for '{1}', it must be a Type or a string.", item.FullName );
                }
                throw new CKException( "Invalid StartDependencySort returned type '{0}' for '{1}', it must be a Type or a string.", item.StartValue.GetType(), item.FullName );
            }
            string typeName = (string)item.StartValue;
            return SimpleTypeFinder.WeakDefault.ResolveType( typeName, true );
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
