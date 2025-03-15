using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup;

/// <summary>
/// Core setup object. Contains the execution context and all auto services required to
/// process a setup. It is in charge of item ordering, setup drivers management and Init/Install/Settle steps.
/// It does not contain anything related to script managemen.
/// </summary>
sealed class SetupCoreEngine : IDisposable
{
    readonly VersionedItemTracker _versionTracker;
    readonly DriverBaseList _allDrivers;
    readonly DriverList _drivers;
    readonly ISetupDriverFactory _driverFactory;
    readonly IActivityMonitor _monitor;
    readonly IServiceProvider _services;
    SetupCoreEngineState _state;

    class DriverBaseList : IDriverBaseList
    {
        readonly Dictionary<object, DriverBase> _index;
        readonly List<DriverBase> _drivers;
        readonly SetupCoreEngine _engine;

        public DriverBaseList( SetupCoreEngine e )
        {
            _engine = e;
            _index = new Dictionary<object, DriverBase>();
            _drivers = new List<DriverBase>();
        }

        public DriverBase? this[string? fullName] => fullName == null ? null : _index.GetValueOrDefault( fullName );

        public DriverBase? this[IDependentItem? item] => item == null ? null : _index.GetValueOrDefault( item );

        public DriverBase this[int index] => _drivers[index];

        public int Count => _drivers.Count;

        public IEnumerator<DriverBase> GetEnumerator() => _drivers.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _drivers.GetEnumerator();

        internal void Clear()
        {
            _index.Clear();
            _drivers.Clear();
        }

        internal void Add( DriverBase d )
        {
            Debug.Assert( d != null && d.Drivers == _engine.Drivers );
            Debug.Assert( !_index.ContainsKey( d.FullName ) );
            Debug.Assert( _drivers.Count == 0 || _drivers[_drivers.Count - 1].SortedItem.Index < d.SortedItem.Index );
            _drivers.Add( d );
            _index.Add( d.FullName, d );
            if( !d.IsGroupHead ) _index.Add( d.Item, d );
        }

    }

    class DriverList : IDriverList
    {
        readonly List<SetupItemDriver> _drivers;
        readonly DriverBaseList _baseList;

        public DriverList( DriverBaseList l )
        {
            _baseList = l;
            _drivers = new List<SetupItemDriver>();
        }

        public SetupItemDriver? this[string fullName] => _baseList[fullName] as SetupItemDriver;

        public SetupItemDriver? this[IDependentItem item] => _baseList[item] as SetupItemDriver;

        public SetupItemDriver this[int index] => _drivers[index];

        public int Count => _drivers.Count;

        public IEnumerator<SetupItemDriver> GetEnumerator() => _drivers.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _drivers.GetEnumerator();

        internal void Clear() => _drivers.Clear();

        internal void Add( SetupItemDriver d ) => _drivers.Add( d );

    }

    /// <summary>
    /// Initializes a new setup engine.
    /// </summary>
    /// <param name="versionTracker">Version tracker.</param>
    /// <param name="services">Available services.</param>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="driverFactory">Factory for setup drivers.</param>
    public SetupCoreEngine( VersionedItemTracker versionTracker, IServiceProvider services, IActivityMonitor monitor, ISetupDriverFactory driverFactory )
    {
        Debug.Assert( versionTracker != null );
        Debug.Assert( services != null );
        Debug.Assert( monitor != null );
        _versionTracker = versionTracker;
        _services = services;
        _driverFactory = driverFactory;
        _monitor = monitor;
        _allDrivers = new DriverBaseList( this );
        _drivers = new DriverList( _allDrivers );
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider Services => _services;

    /// <summary>
    /// Triggered before registration (at the beginning of <see cref="RegisterAndCreateDrivers"/>).
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

    /// <summary>
    /// Gives access to the ordered list (also indexed by item fullname) of all the <see cref="DriverBase"/> that participate to Setup.
    /// DriverBase 
    /// </summary>
    public IDriverBaseList AllDrivers => _allDrivers;

    /// <summary>
    /// Gives access to the ordered list of the <see cref="SetupItemDriver"/>.
    /// </summary>
    public IDriverList Drivers => _drivers;

    /// <summary>
    /// This is the very first step: registers any number of <see cref="IDependentItem"/> and/or <see cref="IDependentItemDiscoverer"/>.
    /// 1 - Raises the <see cref="RegisterSetupEvent"/> event so that external participants can registers other items.
    ///     If cancellation occurs (via <see cref="SetupEventArgs.CancelSetup">RegisterSetupEventArgs.CancelSetup</see>), an error result is returned.
    /// 2 - Raises the <see cref="SetupEvent"/> with its <see cref="SetupEventArgs.Step"/> sets to SetupStep.PreInit
    ///     If cancellation occurs (via <see cref="SetupEventArgs.CancelSetup"/>), an error result is returned.
    /// 3 - Orders all the items topologicaly according to their dependencies/relationships.
    /// 4 - Creates their associated drivers (the type of the driver is given by the <see cref="IDependentItem.StartDependencySort"/> returned value).
    ///     For each newly created drivers, <see cref="DriverEvent"/> is raised with its <see cref="DriverEventArgs.Step"/> sets to SetupStep.PreInit.
    ///     To give the opportunity to external participants that may have prepared stuff if an error or a cancellation occurs once a driver has been created,
    ///     a second <see cref="SetupEvent"/> at PreInitStep indicating the error or cancelation is raised and an error result is returned.
    /// </summary>
    /// <param name="items">Set of <see cref="IDependentItem"/>.</param>
    /// <param name="discoverers">Set of <see cref="IDependentItemDiscoverer"/>.</param>
    /// <param name="options">Optional configuration for dependency graph computation (see <see cref="DependencySorter"/> for more information).</param>
    /// <returns>A <see cref="SetupCoreEngineRegisterResult"/> that captures detailed information about the registration result.</returns>
    public SetupCoreEngineRegisterResult RegisterAndCreateDrivers( IEnumerable<ISetupItem> items, IEnumerable<IDependentItemDiscoverer<ISetupItem>> discoverers, DependencySorterOptions options = null )
    {
        CheckState( SetupCoreEngineState.None );
        var hRegisterSetupEvent = RegisterSetupEvent;
        var hSetupEvent = SetupEvent;
        if( hRegisterSetupEvent != null || hSetupEvent != null )
        {
            var e = new RegisterSetupEventArgs( _monitor );
            try
            {
                if( hRegisterSetupEvent != null )
                {
                    hRegisterSetupEvent( this, e );
                    if( e.CancelReason == null )
                    {
                        if( e.RegisteredItems != null ) items = items.Concat( e.RegisteredItems );
                        if( e.RegisteredDiscoverers != null ) discoverers = discoverers.Concat( e.RegisteredDiscoverers );
                    }
                }
                hSetupEvent?.Invoke( this, e );
                if( e.CancelReason != null )
                {
                    return new SetupCoreEngineRegisterResult( null ) { CancelReason = e.CancelReason };
                }
            }
            catch( Exception ex )
            {
                return new SetupCoreEngineRegisterResult( null ) { UnexpectedError = ex };
            }
        }
        SetupCoreEngineRegisterResult result = null;
        // There is no _state = SetupEngineState.RegistrationError since on error we clear the driver list and
        // the state remains set to SetupEngineState.None.
        try
        {
            result = new SetupCoreEngineRegisterResult( DependencySorter<ISetupItem>.OrderItems( _monitor, items, discoverers, options ) );
            if( result.IsValid )
            {
                #region Creating drivers
                using( _monitor.OpenInfo( $"Instanciating drivers for {result.SortResult.SortedItems.Count} items." ) )
                {
                    foreach( var item in result.SortResult.SortedItems )
                    {
                        SetupItemDriver setupItemDriver = null;
                        DriverBase d;
                        Type typeToCreate = null;
                        if( item.IsGroup )
                        {
                            var head = (GroupHeadSetupDriver)_allDrivers[item.HeadForGroup.FullName];
                            typeToCreate = ResolveDriverType( item );
                            setupItemDriver = CreateSetupDriver( typeToCreate, new SetupItemDriver.BuildInfo( head, _drivers, _allDrivers, item ) );
                            d = head.GroupOrContainer = setupItemDriver;
                        }
                        else
                        {
                            VersionedName externalVersion;
                            IVersionedItem versioned = item.Item as IVersionedItem;
                            if( versioned != null ) externalVersion = _versionTracker.GetCurrent( versioned );
                            else externalVersion = null;

                            if( item.IsGroupHead )
                            {
                                d = new GroupHeadSetupDriver( _drivers, _allDrivers, item, externalVersion );
                            }
                            else
                            {
                                typeToCreate = ResolveDriverType( item );
                                d = setupItemDriver = CreateSetupDriver( typeToCreate, new SetupItemDriver.BuildInfo( _drivers, _allDrivers, item, externalVersion ) );
                            }
                        }
                        Debug.Assert( d != null, "Otherwise an exception is thrown by CreateSetupDriver that will be caught as the result.UnexpectedError." );
                        _allDrivers.Add( d );
                        if( setupItemDriver != null ) _drivers.Add( setupItemDriver );
                    }
                }
                #endregion

                #region Pre initialization phasis.
                using( _monitor.OpenInfo( $"Calling PreInit on {_allDrivers.Count} drivers." ) )
                {
                    var reusableEvent = new DriverEventArgs( _monitor, SetupStep.PreInit );
                    foreach( var d in _allDrivers )
                    {
                        Debug.Assert( (d is SetupItemDriver) == !d.IsGroupHead, $"There is only 2 DriverBase specializations: {nameof( SetupItemDriver )} and {nameof( GroupHeadSetupDriver )}." );
                        // Raising PreInit global event.
                        var hE = DriverEvent;
                        if( hE != null )
                        {
                            reusableEvent.Driver = d;
                            hE( this, reusableEvent );
                            if( reusableEvent.CancelSetup )
                            {
                                result.CanceledRegistrationCulprit = d.SortedItem;
                                _allDrivers.Clear();
                                return result;
                            }
                        }
                        // Calling ExecutePreInit.
                        if( !d.IsGroupHead )
                        {
                            SetupItemDriver genDriver = (SetupItemDriver)d;
                            if( !genDriver.ExecutePreInit( _monitor ) )
                            {
                                string msg = $"Canceled by '{d.Item.FullName}'.ExecutePreInit() method.";
                                return new SetupCoreEngineRegisterResult( null ) { CancelReason = msg };
                            }
                            IStObjSetupItem stObjIem = d.Item as IStObjSetupItem;
                            if( stObjIem != null && stObjIem.StObj != null )
                            {
                                var all = stObjIem.StObj.Attributes.GetAllCustomAttributes<ISetupItemDriverAware>();
                                foreach( var a in all )
                                {
                                    if( !a.OnDriverPreInitialized( _monitor, genDriver ) )
                                    {
                                        string msg = $"Canceled by one Attribute of Item '{d.Item.FullName}' during driver creation.";
                                        return new SetupCoreEngineRegisterResult( null ) { CancelReason = msg };
                                    }
                                }
                            }
                            ISetupItemDriverAware aware = d.Item as ISetupItemDriverAware;
                            if( aware != null )
                            {
                                if( !aware.OnDriverPreInitialized( _monitor, genDriver ) )
                                {
                                    string msg = $"Canceled by Item '{d.Item.FullName}' during driver creation.";
                                    return new SetupCoreEngineRegisterResult( null ) { CancelReason = msg };
                                }
                            }
                        }
                    }
                }
                #endregion
            }
        }
        catch( Exception ex )
        {
            // Exception is not logged at this level: it is carried by the SetupEngineRegisterResult
            // and its LogError method must be used to log different kind of errors.
            if( result == null ) result = new SetupCoreEngineRegisterResult( null );
            result.UnexpectedError = ex;
            _allDrivers.Clear();
        }
        if( result.IsValid ) _state = SetupCoreEngineState.Registered;
        else
        {
            SafeFireSetupEvent( SetupStep.PreInit, errorOccured: true );
        }
        return result;
    }

    SetupItemDriver CreateSetupDriver( Type typeToCreate, SetupItemDriver.BuildInfo buildInfo )
    {
        if( _driverFactory != null )
        {
            try
            {
                return _driverFactory.CreateDriver( typeToCreate, buildInfo );
            }
            catch( Exception ex )
            {
                throw new CKException( ex, $"While creating SetupDriver for item '{buildInfo.SortedItem.FullName}', type='{typeToCreate.FullName}'." );
            }
        }
        var d = (SetupItemDriver)_services.SimpleObjectCreate( _monitor, typeToCreate, buildInfo );
        if( d == null )
        {
            throw new CKException( $"Unable to create SetupDriver for item '{buildInfo.SortedItem.FullName}', type='{typeToCreate.FullName}'." );
        }
        return d;
    }

    bool SafeFireSetupEvent( SetupStep step, bool errorOccured = false )
    {
        var h = SetupEvent;
        if( h == null ) return true;
        using( _monitor.OpenTrace( errorOccured ? $"Raising error event during {step}." : $"Raising {step} setup event." ) )
        {
            var e = new SetupEventArgs( _monitor, step, errorOccured );
            try
            {
                h( this, e );
                if( e.CancelReason == null ) return true;
                _monitor.Fatal( e.CancelReason );
            }
            catch( Exception ex )
            {
                _monitor.Fatal( ex );
            }
        }
        return false;
    }

    /// <summary>
    /// This is the second step: called after a successful call to <see cref="RegisterAndCreateDrivers"/>.
    /// 1 - Raises the <see cref="SetupEvent"/> at step <see cref="SetupStep.Init"/>.
    ///     If a cancellation occurs, returns false.
    /// 2 - For each drivers:
    ///     - Call <see cref="DriverBase.ExecuteInit"/>. It it
    /// </summary>
    /// <returns></returns>
    public bool RunInit()
    {
        CheckState( SetupCoreEngineState.Registered );
        _state = SetupCoreEngineState.InitializationError;
        if( !SafeFireSetupEvent( SetupStep.Init ) ) return false;
        try
        {
            var reusableEvent = new DriverEventArgs( _monitor, SetupStep.Init );
            foreach( var d in _allDrivers )
            {
                using( _monitor.OpenInfo( $"Initializing {d.FullName}" ) )
                {
                    if( !d.ExecuteInit( _monitor ) ) return false;
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
            _monitor.Fatal( ex );
            SafeFireSetupEvent( SetupStep.Init, true );
            return false;
        }
        _state = SetupCoreEngineState.Initialized;
        return true;
    }

    public bool RunInstall()
    {
        CheckState( SetupCoreEngineState.Initialized );
        _state = SetupCoreEngineState.InstallationError;
        if( !SafeFireSetupEvent( SetupStep.Install ) ) return false;
        try
        {
            var reusableEvent = new DriverEventArgs( _monitor, SetupStep.Install );
            foreach( var d in _allDrivers )
            {
                using( _monitor.OpenInfo( $"Installing {d.FullName} ({VersionTransitionString( d )})." ) )
                {
                    if( !d.ExecuteInstall( _monitor ) ) return false;
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
            _monitor.Fatal( ex );
            SafeFireSetupEvent( SetupStep.Install, true );
            return false;
        }
        _state = SetupCoreEngineState.Installed;
        return true;
    }

    private static string VersionTransitionString( DriverBase d )
    {
        string versionTransition;
        if( d.ItemVersion == null )
        {
            versionTransition = "unversioned";
        }
        else
        {
            if( d.ExternalVersion == null )
            {
                versionTransition = String.Format( "¤ => {0}", d.ItemVersion );
            }
            else
            {
                if( d.ExternalVersion.Version == d.ItemVersion )
                {
                    versionTransition = String.Format( "= {0} =", d.ItemVersion );
                }
                else
                {
                    if( d.IsGroupHead ) d = ((GroupHeadSetupDriver)d).GroupOrContainer;
                    if( d.ExternalVersion.FullName != d.FullName )
                    {
                        versionTransition = String.Format( "{0} => {1}", d.ExternalVersion, d.ItemVersion );
                    }
                    else
                    {
                        versionTransition = String.Format( "{0} => {1}", d.ExternalVersion.Version, d.ItemVersion );
                    }
                }
            }
        }
        return versionTransition;
    }

    public bool RunSettle()
    {
        CheckState( SetupCoreEngineState.Installed );
        _state = SetupCoreEngineState.SettlementError;
        if( !SafeFireSetupEvent( SetupStep.Settle ) ) return false;
        try
        {
            var reusableEvent = new DriverEventArgs( _monitor, SetupStep.Settle );
            foreach( var d in _allDrivers )
            {
                using( _monitor.OpenInfo( $"Settling {d.FullName}." ) )
                {
                    if( !d.ExecuteSettle( _monitor ) ) return false;
                    var hE = DriverEvent;
                    if( hE != null )
                    {
                        reusableEvent.Driver = d;
                        hE( this, reusableEvent );
                        if( reusableEvent.CancelSetup ) return false;
                    }
                    if( !(d is GroupHeadSetupDriver) )
                    {
                        IVersionedItem versioned = d.Item as IVersionedItem;
                        if( versioned != null ) _versionTracker.SetCurrent( versioned );
                        else _versionTracker.Delete( d.FullName );
                    }
                }
            }
        }
        catch( Exception ex )
        {
            _monitor.Fatal( ex );
            SafeFireSetupEvent( SetupStep.Settle, true );
            return false;
        }
        _state = SetupCoreEngineState.Settled;
        SafeFireSetupEvent( SetupStep.Success );
        return true;
    }

    public void Dispose()
    {
        if( (_state & SetupCoreEngineState.Disposed) == 0 )
        {
            using( _monitor.OpenInfo( $"Disposing {_allDrivers.Count} drivers." ) )
            {
                foreach( var d in _allDrivers )
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
                            _monitor.Error( $"Disposing {d.FullName} of type '{d.GetType()}'.", ex );
                        }
                    }
                }
            }
            _state |= SetupCoreEngineState.Disposed;
            SafeFireSetupEvent( SetupStep.Disposed );
        }
    }

    private static Type ResolveDriverType( ISortedItem item )
    {
        if( item.StartValue is Type ) return (Type)item.StartValue;
        if( !(item.StartValue is string) )
        {
            if( item.StartValue == null )
            {
                return typeof( SetupItemDriver );
            }
            throw new CKException( $"StartDependencySort() method returned type '{item.StartValue.GetType()}' for '{item.FullName}', it must be a Type or a string." );
        }
        string typeName = (string)item.StartValue;
        return SimpleTypeFinder.WeakResolver( typeName, true );
    }

    void CheckState( SetupCoreEngineState requiredState )
    {
        if( _state != requiredState )
        {
            throw new InvalidOperationException( $"Invalid SetupCenter state: {requiredState} expected but was {_state.ToString()}." );
        }
    }

}
