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
    /// this class and offers package script support (see <see cref="SetupEngine.ScriptTypeManager"/> and <see cref="SetupEngine.Scripts"/>).
    /// </summary>
    sealed class SetupCoreEngine : ISetupEngine, IDisposable
    {
        readonly IVersionedItemRepository _versionRepository;
        readonly DriverList _drivers;
        readonly ISetupDriverFactory _driverFactory;
        readonly IActivityMonitor _monitor;
        readonly ISetupSessionMemory _memory;
        readonly IReadOnlyList<ISetupEngineAspect> _aspects;
        SetupEngineState _state;

        class DriverList : IDriverList
        {
            Dictionary<object,DriverBase> _index;
            List<DriverBase> _drivers;
            SetupCoreEngine _center;

            public DriverList( SetupCoreEngine center )
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

            GenericItemSetupDriver ISetupDriverFactory.CreateDriver( Type type, GenericItemSetupDriver.BuildInfo info )
            {
                return (GenericItemSetupDriver)Activator.CreateInstance( type, info );
            }
        }

        /// <summary>
        /// Initializes a new setup engine.
        /// </summary>
        /// <param name="versionRepository">Provides version information about items already installed.</param>
        /// <param name="memory">Provides persistent memory to setup participants.</param>
        /// <param name="_monitor">Monitor to use.</param>
        /// <param name="driverFactory">Factory for setup drivers.</param>
        public SetupCoreEngine( IVersionedItemRepository versionRepository, ISetupSessionMemory memory, IReadOnlyList<ISetupEngineAspect> aspects, IActivityMonitor monitor, ISetupDriverFactory driverFactory )
        {
            Debug.Assert( versionRepository != null );
            Debug.Assert( aspects != null );
            Debug.Assert( monitor != null );
            Debug.Assert( memory != null );
            _versionRepository = versionRepository;
            _memory = memory;
            _aspects = aspects;
            _driverFactory = driverFactory ?? DefaultDriverfactory.Default;
            _monitor = monitor;
            _drivers = new DriverList( this );
        }

        /// <summary>
        /// Gets the <see cref="ISetupEngineAspect"/> that participate to setup.
        /// </summary>
        public IReadOnlyList<ISetupEngineAspect> Aspects 
        {
            get { return _aspects; } 
        }

        /// <summary>
        /// Gets the first typed aspect that is assignable to <typeparamref name="T"/>. 
        /// If such aspect can not be found, a <see cref="CKException"/> is thrown.
        /// </summary>
        /// <typeparam name="T">Type of the aspect to obtain.</typeparam>
        /// <returns>The first compatible aspect.</returns>
        public T Aspect<T>()
        {
            T a = _aspects.OfType<T>().FirstOrDefault();
            if( a == null ) throw new CKException( "Aspect '{0}' is required. Did you forget to ragister an aspect configuration in the SetupEngineConfiguration.Aspects list?", typeof(T).FullName );
            return a;
        }

        /// <summary>
        /// Triggered before registration (at the beginning of <see cref="Register"/>).
        /// This event fires before the <see cref="SetupEvent"/> (with <see cref="SetupEvent.Step"/> set to None), and enables
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
        
        public ISetupSessionMemory Memory
        {
            get { return _memory; }
        }

        public IActivityMonitor Monitor
        {
            get { return _monitor; }
        }

        /// <summary>
        /// Gives access to the ordered list of all the <see cref="DriverBase"/> that participate to Setup.
        /// This list is filled after <see cref="RegisterSetupEvent"/> (and <see cref="SetupEvent"/> with <see cref="SetupStep.PreInit"/>) but before <see cref="SetupStep.Init"/>.
        /// </summary>
        public IDriverList AllDrivers
        {
            get { return _drivers; }
        }

        /// <summary>
        /// Gets the current state of the engine.
        /// </summary>
        public SetupEngineState State
        {
            get { return _state; }
        }

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
        public SetupCoreEngineRegisterResult Register( IEnumerable<ISetupItem> items, IEnumerable<IDependentItemDiscoverer<ISetupItem>> discoverers, DependencySorterOptions options = null )
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
                        GenericItemSetupDriver setupItemDriver = null;
                        DriverBase d;
                        Type typeToCreate = null;
                        if( item.IsGroup )
                        {
                            var head = (GroupHeadSetupDriver)_drivers[item.HeadForGroup.FullName];
                            typeToCreate = ResolveDriverType( item );
                            setupItemDriver = CreateSetupDriver( typeToCreate, new GenericItemSetupDriver.BuildInfo( head, item ) );
                            d = head.Group = setupItemDriver;
                        }
                        else
                        {
                            VersionedName externalVersion;
                            IVersionedItem versioned = item.Item as IVersionedItem;
                            if( versioned != null ) externalVersion = _versionRepository.GetCurrent( versioned );
                            else externalVersion = null;

                            if( item.IsGroupHead )
                            {
                                d = new GroupHeadSetupDriver( this, item, externalVersion );
                            }
                            else
                            {
                                typeToCreate = ResolveDriverType( item );
                                d = setupItemDriver = CreateSetupDriver( typeToCreate, new GenericItemSetupDriver.BuildInfo( this, item, externalVersion ) );
                            }
                        }
                        Debug.Assert( d != null, "Otherwise an exception is thrown by CreateSetupDriver that will be caught as the result.UnexpectedError." );
                        _drivers.Add( d );
                        if( setupItemDriver != null )
                        {
                            if( !item.Item.OnDriverCreated( setupItemDriver ) )
                            {
                                string msg = String.Format( "Canceled by Item '{0}' on driver creation.", item.Item.FullName );
                                return new SetupCoreEngineRegisterResult( null ) { CancelReason = msg };
                            }
                        }
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
                if( result == null ) result = new SetupCoreEngineRegisterResult( null );
                result.UnexpectedError = ex;
                _drivers.Clear();
            }
            if( result.IsValid ) _state = SetupEngineState.Registered;
            else
            {
                SafeFireSetupEvent( SetupStep.PreInit, errorOccured: true );
            }
            return result;
        }

        GenericItemSetupDriver CreateSetupDriver( Type typeToCreate, GenericItemSetupDriver.BuildInfo buildInfo )
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
        /// This is the second step: called after a successful call to <see cref="Register"/>.
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
                foreach( var d in _drivers )
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
                foreach( var d in _drivers )
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
                foreach( var d in _drivers )
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
                        IVersionedItem versioned = d.Item as IVersionedItem;
                        if( versioned != null ) _versionRepository.SetCurrent( versioned );
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
                using( Monitor.OpenInfo().Send( "Disposing {0} drivers.", _drivers.Count ) )
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
