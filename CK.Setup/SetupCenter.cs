using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class SetupCenter
    {
        readonly IVersionedItemRepository _versionRepository;
        readonly DriverList _drivers;
        readonly ISetupDriverFactory _driverFactory;
        readonly IActivityLogger _logger;
        SetupCenterState _state;

        class DriverList : ISetupDriverList
        {
            Dictionary<string,SetupDriverBase> _byName;
            List<SetupDriverBase> _drivers;
            SetupCenter _center;

            public DriverList( SetupCenter center )
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
                return d != null ? d.SetupCenter == _center : false;
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
                Debug.Assert( d != null && d.SetupCenter == _center );
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

        public SetupCenter( IVersionedItemRepository versionRepository, IActivityLogger logger, ISetupDriverFactory driverFactory )
        {
            if( versionRepository == null ) throw new ArgumentNullException( "versionRepository" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
            _versionRepository = versionRepository;
            _driverFactory = driverFactory ?? DefaultDriverfactory.Default;
            _logger = logger;
            _drivers = new DriverList( this );
        }

        public event EventHandler<SetupDriverEventArgs> DriverEvent;

        public IVersionedItemRepository VersionRepository
        {
            get { return _versionRepository; }
        }

        public IActivityLogger Logger
        {
            get { return _logger; }
        }

        public ISetupDriverList Drivers
        {
            get { return _drivers; }
        }

        public bool RegisterDone
        {
            get { return _drivers.Count > 0; }
        }

        public SetupCenterState State
        {
            get { return _state; }
        }

        public SetupCenterRegisterResult Register( params object[] items )
        {
            return Register( items.OfType<ISetupableItem>(), items.OfType<IDependentItemDiscoverer>() );
        }

        public SetupCenterRegisterResult Register( IEnumerable<ISetupableItem> items, IEnumerable<IDependentItemDiscoverer> discoverers, bool reverseName = false )
        {
            CheckState( SetupCenterState.None );
            SetupCenterRegisterResult result = null;
            try
            {
                result = new SetupCenterRegisterResult( DependencySorter.OrderItems( items, discoverers, reverseName ) );
                if( result.IsValid )
                {
                    var reusableEvent = new SetupDriverEventArgs( SetupStep.None );
                    foreach( var item in result.SortResult.SortedItems )
                    {
                        SetupDriverBase d;
                        if( item.IsContainer )
                        {
                            var head = _drivers[item.HeadForContainer.FullName] as SetupDriverHead;
                            Debug.Assert( head != null );
                            Type containerType = ResolveDriverType( item );
                            SetupDriverContainer c = _driverFactory.CreateDriverContainer( containerType, new SetupDriverContainer.BuildInfo( head, item ) );
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
                                Type driverType = ResolveDriverType( item );
                                d = _driverFactory.CreateDriver( driverType, new SetupDriver.BuildInfo( this, item, externalVersion ) );
                            }
                        }
                        _drivers.Add( d );
                        var hE = DriverEvent;
                        if( hE != null )
                        {
                            reusableEvent.Driver = d;
                            hE( this, reusableEvent );
                        }
                    }
                    _state = SetupCenterState.Registered;
                }
            }
            catch( Exception ex )
            {
                if( result == null ) result = new SetupCenterRegisterResult( null );
                _drivers.Clear();
                result.UnexpectedError = ex;
            }
            return result;
        }

        public bool RunInit()
        {
            CheckState( SetupCenterState.Registered );
            _state = SetupCenterState.InitializationError;
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
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                _logger.Error( ex.Message );
                return false;
            }
            _state = SetupCenterState.Initialized;
            return true;
        }

        public bool RunInstall()
        {
            CheckState( SetupCenterState.Initialized );
            _state = SetupCenterState.InstallationError;
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
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                _logger.Error( ex.Message );
                return false;
            }
            _state = SetupCenterState.Installed;
            return true;
        }

        public bool RunSettle()
        {
            CheckState( SetupCenterState.Installed );
            _state = SetupCenterState.SettlementError;
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
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                _logger.Error( ex.Message );
                return false;
            }

            _state = SetupCenterState.Settled;
            return true;
        }

        private static Type ResolveDriverType( ISortedItem item )
        {
            string typeName = ((ISetupableItem)item.Item).SetupDriverTypeName;
            Type t = SimpleTypeFinder.Default.ResolveType( typeName, true );
            return t;
        }

        void CheckState( SetupCenterState requiredState )
        {
            if( _state != requiredState )
            {
                throw new InvalidOperationException( String.Format( "Invalid SetupCenter state: {0} expected but was {1}", requiredState, _state.ToString() ) );
            }
        }

    }
}
