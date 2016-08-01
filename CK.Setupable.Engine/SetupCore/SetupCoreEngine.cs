#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\SetupCore\SetupEngine.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
    /// process a setup. It is in charge of item ordering, setup drivers management and Init/Install/Settle steps.
    /// It does not contain anything related to script management: the <see cref="SetupEngine"/> wraps
    /// this class and offers package script support (see <see cref="SetupEngine.Scripts"/>).
    /// </summary>
    sealed class SetupCoreEngine : ISetupEngine, IDisposable
    {
        readonly VersionedItemTracker _versionTracker;
        readonly DriverBaseList _allDrivers;
        readonly DriverList _genDrivers;
        readonly ISetupDriverFactory _driverFactory;
        readonly IActivityMonitor _monitor;
        readonly ISetupSessionMemory _memory;
        readonly IReadOnlyList<ISetupEngineAspect> _aspects;
        SetupEngineState _state;

        class DriverBaseList : IDriverBaseList
        {
            Dictionary<object,DriverBase> _index;
            List<DriverBase> _drivers;
            SetupCoreEngine _center;

            public DriverBaseList( SetupCoreEngine center )
            {
                _center = center;
                _index = new Dictionary<object, DriverBase>();
                _drivers = new List<DriverBase>();
            }

            public DriverBase this[string fullName]
            {
                get { return fullName == null ? null : _index.GetValueWithDefault( fullName, null ); }
            }

            public DriverBase this[ IDependentItem item ]
            {
                get { return item == null ? null : _index.GetValueWithDefault( item, null ); }
            }

            public DriverBase this[int index]
            {
                get { return _drivers[index]; }
            }

            public int Count
            {
                get { return _drivers.Count; }
            }

            public IEnumerator<DriverBase> GetEnumerator() => _drivers.GetEnumerator();

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _drivers.GetEnumerator();

            internal void Clear()
            {
                _index.Clear();
                _drivers.Clear();
            }

            internal void Add( DriverBase d )
            {
                Debug.Assert( d != null && d.Engine == _center );
                Debug.Assert( !_index.ContainsKey( d.FullName ) );
                Debug.Assert( _drivers.Count == 0 || _drivers[_drivers.Count-1].SortedItem.Index < d.SortedItem.Index );
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

            public SetupItemDriver this[string fullName] =>  _baseList[fullName] as SetupItemDriver; 

            public SetupItemDriver this[IDependentItem item] => _baseList[item] as SetupItemDriver; 

            public SetupItemDriver this[int index] => _drivers[index]; 

            public int Count => _drivers.Count; 

            public IEnumerator<SetupItemDriver> GetEnumerator() => _drivers.GetEnumerator();

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _drivers.GetEnumerator();

            internal void Clear() => _drivers.Clear();

            internal void Add( SetupItemDriver d ) => _drivers.Add( d );

        }

        class DefaultDriverfactory : ISetupDriverFactory
        {
            public readonly static ISetupDriverFactory Default = new DefaultDriverfactory();

            SetupItemDriver ISetupDriverFactory.CreateDriver( Type type, SetupItemDriver.BuildInfo info )
            {
                return (SetupItemDriver)Activator.CreateInstance( type, info );
            }
        }

        /// <summary>
        /// Initializes a new setup engine.
        /// </summary>
        /// <param name="versionRepository">Provides version information about items already installed.</param>
        /// <param name="memory">Provides persistent memory to setup participants.</param>
        /// <param name="aspects">Available aspects.</param>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="driverFactory">Factory for setup drivers.</param>
        public SetupCoreEngine( VersionedItemTracker versionTracker, ISetupSessionMemory memory, IReadOnlyList<ISetupEngineAspect> aspects, IActivityMonitor monitor, ISetupDriverFactory driverFactory )
        {
            Debug.Assert( versionTracker != null );
            Debug.Assert( aspects != null );
            Debug.Assert( monitor != null );
            Debug.Assert( memory != null );
            _versionTracker = versionTracker;
            _memory = memory;
            _aspects = aspects;
            _driverFactory = driverFactory ?? DefaultDriverfactory.Default;
            _monitor = monitor;
            _allDrivers = new DriverBaseList( this );
            _genDrivers = new DriverList( _allDrivers );
        }

        /// <summary>
        /// Gets the <see cref="ISetupEngineAspect"/> that participate to setup.
        /// </summary>
        public IReadOnlyList<ISetupEngineAspect> Aspects => _aspects; 

        /// <summary>
        /// Gets the first typed aspect that is assignable to <typeparamref name="T"/>. 
        /// If such aspect can not be found, depending on <paramref name="required"/> a <see cref="CKException"/> is thrown or null is returned.
        /// </summary>
        /// <typeparam name="T">Type of the aspect to obtain.</typeparam>
        /// <param name="required">False to silently return null instead of throwing an exception if the aspect can not be found.</param>
        /// <returns>The first compatible aspect (may be null if <paramref name="required"/> is false).</returns>
        public T GetSetupEngineAspect<T>( bool required = true ) where T : class
        {
            return SetupEngine.GetSetupEngineAspect<T>( _aspects, required );
        }

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
        /// Gets the <see cref="ISetupSessionMemory"/> service that is used to persist any state related to setup phasis.
        /// It is a simple key-value dictionary where key is a string not longer than 255 characters and value is a non null string.
        /// </summary>
        public ISetupSessionMemory Memory => _memory; 

        /// <summary>
        /// Monitor that will be used during setup.
        /// </summary>
        public IActivityMonitor Monitor => _monitor; 

        /// <summary>
        /// Gives access to the ordered list (also indexed by item fullname) of all the <see cref="DriverBase"/> that participate to Setup.
        /// DriverBase 
        /// This list is filled after <see cref="RegisterSetupEvent"/> (and <see cref="SetupEvent"/> with <see cref="SetupStep.PreInit"/>) but before <see cref="SetupStep.Init"/>.
        /// </summary>
        public IDriverBaseList AllDrivers => _allDrivers; 

        /// <summary>
        /// Gives access to the ordered list of the <see cref="SetupItemDriver"/>.
        /// </summary>
        public IDriverList Drivers => _genDrivers; 

        /// <summary>
        /// Gets the current state of the engine.
        /// </summary>
        public SetupEngineState State => _state; 

        /// <summary>
        /// This is the very first step: registers any number of <see cref="IDependentItem"/> and/or <see cref="IDependentItemDiscoverer"/>.
        /// 1 - Raises the <see cref="RegisterSetupEvent"/> event so that external participants can registers other items.
        ///     If cancellation occurs (via <see cref="SetupEventArgs.CancelSetup">RegisterSetupEventArgs.CancelSetup</see>), an error result is returned.
        /// 2 - Raises the <see cref="SetupEvent"/> with its <see cref="SetupEventArgs.Step"/> sets to SetupStep.PreInit
        ///     If cancellation occurs (via <see cref="SetupEventArgs.CancelSetup"/>), an error result is returned.
        /// 3 - Orders all the items topologicaly according to their dependencies/relationships.
        /// 4 - Creates their associated drivers (the type of the driver is given by the <see cref="IDependentItem.StartDependencySort"/> returned value).
        ///     For each newly created drivers, <see cref="DriverEvent"/> is raised with its <see cref="DriverEventArgs.Step"/> sets to SetupStep.PreInit.
        ///     To give the opportunity to external participants that may have prepared stuff if an error or a concellation occurs once a driver has been created,
        ///     a second <see cref="SetupEvent"/> at PreInitStep indicating the error or cancelation is raised and an error result is returned.
        /// </summary>
        /// <param name="items">Set of <see cref="IDependentItem"/>.</param>
        /// <param name="discoverers">Set of <see cref="IDependentItemDiscoverer"/>.</param>
        /// <param name="options">Optional configuration for dependency graph computation (see <see cref="DependencySorter"/> for more information).</param>
        /// <returns>A <see cref="SetupCoreEngineRegisterResult"/> that captures detailed information about the registration result.</returns>
        public SetupCoreEngineRegisterResult RegisterAndCreateDrivers( IEnumerable<ISetupItem> items, IEnumerable<IDependentItemDiscoverer<ISetupItem>> discoverers, DependencySorterOptions options = null )
        {
            CheckState( SetupEngineState.None );
            var hRegisterSetupEvent = RegisterSetupEvent;
            var hSetupEvent = SetupEvent;
            if( hRegisterSetupEvent != null || hSetupEvent != null )
            {
                var e = new RegisterSetupEventArgs();
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
                    if( hSetupEvent != null ) hSetupEvent( this, e );
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
                result = new SetupCoreEngineRegisterResult( DependencySorter<ISetupItem>.OrderItems( items, discoverers, options ) );
                if( result.IsValid )
                {
                    var reusableEvent = new DriverEventArgs( SetupStep.PreInit );
                    foreach( var item in result.SortResult.SortedItems )
                    {
                        SetupItemDriver setupItemDriver = null;
                        DriverBase d;
                        Type typeToCreate = null;
                        if( item.IsGroup )
                        {
                            var head = (GroupHeadSetupDriver)_allDrivers[item.HeadForGroup.FullName];
                            typeToCreate = ResolveDriverType( item );
                            setupItemDriver = CreateSetupDriver( typeToCreate, new SetupItemDriver.BuildInfo( head, item ) );
                            d = head.Group = setupItemDriver;
                        }
                        else
                        {
                            VersionedName externalVersion;
                            IVersionedItem versioned = item.Item as IVersionedItem;
                            if( versioned != null ) externalVersion = _versionTracker.GetCurrent( versioned );
                            else externalVersion = null;

                            if( item.IsGroupHead )
                            {
                                d = new GroupHeadSetupDriver( this, item, externalVersion );
                            }
                            else
                            {
                                typeToCreate = ResolveDriverType( item );
                                d = setupItemDriver = CreateSetupDriver( typeToCreate, new SetupItemDriver.BuildInfo( this, item, externalVersion ) );
                            }
                        }
                        Debug.Assert( d != null, "Otherwise an exception is thrown by CreateSetupDriver that will be caught as the result.UnexpectedError." );
                        _allDrivers.Add( d );
                        if( setupItemDriver != null ) _genDrivers.Add( setupItemDriver );
                        var hE = DriverEvent;
                        if( hE != null )
                        {
                            reusableEvent.Driver = d;
                            hE( this, reusableEvent );
                            if( reusableEvent.CancelSetup )
                            {
                                result.CanceledRegistrationCulprit = item;
                                _allDrivers.Clear();
                                break;
                            }
                        }
                    }
                }
                foreach( var d in _allDrivers )
                {
                    if( !d.IsGroupHead )
                    {
                        SetupItemDriver genDriver = (SetupItemDriver)d;
                        IStObjSetupItem stObjIem  = d.Item as IStObjSetupItem;
                        if( stObjIem != null && stObjIem.StObj != null )
                        {
                            var all = stObjIem.StObj.Attributes.GetAllCustomAttributes<ISetupItemDriverAware>();
                            foreach( var a in all )
                            {
                                if( !a.OnDriverCreated( genDriver ) )
                                {
                                    string msg = String.Format( "Canceled by one Attribute of Item '{0}' during driver creation.", d.Item.FullName );
                                    return new SetupCoreEngineRegisterResult( null ) { CancelReason = msg };
                                }
                            }
                        }
                        ISetupItemDriverAware aware = d.Item as ISetupItemDriverAware;
                        if( aware != null )
                        {
                            if( !aware.OnDriverCreated( genDriver ) )
                            {
                                string msg = String.Format( "Canceled by Item '{0}' during driver creation.", d.Item.FullName );
                                return new SetupCoreEngineRegisterResult( null ) { CancelReason = msg };
                            }
                        }
                    }
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
            if( result.IsValid ) _state = SetupEngineState.Registered;
            else
            {
                SafeFireSetupEvent( SetupStep.PreInit, errorOccured: true );
            }
            return result;
        }

        SetupItemDriver CreateSetupDriver( Type typeToCreate, SetupItemDriver.BuildInfo buildInfo )
        {
            try
            {
                return _driverFactory.CreateDriver( typeToCreate, buildInfo ) ?? DefaultDriverfactory.Default.CreateDriver( typeToCreate, buildInfo );
            }
            catch( Exception ex )
            {
                throw new CKException( ex, "While creating SetupDriver for item '{1}', type='{0}'.", typeToCreate.FullName, buildInfo.SortedItem.FullName );
            }
        }

        bool SafeFireSetupEvent( SetupStep step, bool errorOccured = false )
        {
            var h = SetupEvent;
            if( h == null ) return true;
            using( _monitor.OpenTrace().Send( errorOccured ? "Raising error event during {0}." : "Raising {0} setup event.", step ) )
            {
                var e = new SetupEventArgs( step, errorOccured );
                try
                {
                    h( this, e );
                    if( e.CancelReason == null ) return true;
                    _monitor.Fatal().Send( e.CancelReason );
                }
                catch( Exception ex )
                {
                    _monitor.Fatal().Send( ex );
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
            CheckState( SetupEngineState.Registered );
            _state = SetupEngineState.InitializationError;
            if( !SafeFireSetupEvent( SetupStep.Init ) ) return false;
            try
            {
                var reusableEvent = new DriverEventArgs( SetupStep.Init );
                foreach( var d in _allDrivers )
                {
                    using( _monitor.OpenInfo().Send( "Initializing {0}", d.FullName ) )
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
                _monitor.Fatal().Send( ex );
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
                foreach( var d in _allDrivers )
                {
                    using( _monitor.OpenInfo().Send( "Installing {0} ({1})", d.FullName, VersionTransitionString( d ) ) )
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
                _monitor.Fatal().Send( ex );
                SafeFireSetupEvent( SetupStep.Install, true );
                return false;
            }
            _state = SetupEngineState.Installed;
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
                        if( d.IsGroupHead ) d = ((GroupHeadSetupDriver)d).Group;
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
            CheckState( SetupEngineState.Installed );
            _state = SetupEngineState.SettlementError;
            if( !SafeFireSetupEvent( SetupStep.Settle ) ) return false;
            try
            {
                var reusableEvent = new DriverEventArgs( SetupStep.Settle );
                foreach( var d in _allDrivers )
                {
                    using( _monitor.OpenInfo().Send( "Settling {0}", d.FullName ) )
                    {
                        if( !d.ExecuteSettle() ) return false;
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
                _monitor.Fatal().Send( ex );
                SafeFireSetupEvent( SetupStep.Settle, true );
                return false;
            }
            _state = SetupEngineState.Settled;
            SafeFireSetupEvent( SetupStep.Success );
            return true;
        }

        public void Dispose()
        {
            if( (_state & SetupEngineState.Disposed) == 0 )
            {
                using( Monitor.OpenInfo().Send( "Disposing {0} drivers.", _allDrivers.Count ) )
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
                                Monitor.Error().Send( ex, "Disposing {0} of type '{1}'.", d.FullName, d.GetType() );
                            }
                        }
                    }
                }
                _state |= SetupEngineState.Disposed;
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

        void CheckState( SetupEngineState requiredState )
        {
            if( _state != requiredState )
            {
                throw new InvalidOperationException( String.Format( "Invalid SetupCenter state: {0} expected but was {1}", requiredState, _state.ToString() ) );
            }
        }

    }
}
