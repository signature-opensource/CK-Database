#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\StObj\StObjSetupItemBuilder.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Collections;
using System.Reflection;

namespace CK.Setup
{
    class StObjSetupItemBuilder
    {
        readonly IActivityMonitor _monitor;
        readonly IStObjSetupConfigurator _configurator;
        readonly IStObjSetupItemFactory _setupItemFactory;
        readonly IStObjSetupDynamicInitializer _dynamicInitializer;
        readonly IServiceProvider _services;

        public StObjSetupItemBuilder( IActivityMonitor monitor, IServiceProvider services, IStObjSetupConfigurator configurator = null, IStObjSetupItemFactory setupItemFactory = null, IStObjSetupDynamicInitializer dynamicInitializer = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            _monitor = monitor;
            _configurator = configurator;
            _setupItemFactory = setupItemFactory;
            _dynamicInitializer = dynamicInitializer;
            _services = services;
        }

        /// <summary>
        /// Initializes a set of <see cref="ISetupItem"/> given a dependency-ordered list of <see cref="IStObjResult"/> objects.
        /// </summary>
        /// <param name="orderedObjects">Root <see cref="IStObjResult"/> objects.</param>
        /// <returns>A set of setup items.</returns>
        public IEnumerable<ISetupItem> Build( IReadOnlyList<IStObjResult> orderedObjects )
        {
            if( orderedObjects == null ) throw new ArgumentNullException( nameof( orderedObjects ) );
            bool hasError = false;
            using( _monitor.OnError( () => hasError = true ) )
            using( _monitor.OpenInfo( "Creating Setup Items from Structured Objects." ) )
            {
                var setupableItems = new Dictionary<IStObjResult, StObjSetupData>();
                BuildSetupItems( orderedObjects, setupableItems );
                BindDependencies( setupableItems );
                if( !CallDynamicInitializer( orderedObjects, setupableItems ) )
                {
                    Debug.Assert( hasError, "An error has been logged." );
                    return null;
                }
                return setupableItems.Values.Select( data => data.SetupItem );
            }
        }

        #region SafeBuildStObj phasis: BuildSetupItems, BindDependencies and CallDynamicInitializer

        void BuildSetupItems( IReadOnlyList<IStObjResult> orderedObjects, Dictionary<IStObjResult, StObjSetupData> setupableItems )
        {
            using( _monitor.OpenInfo( $"Building setupable items from {orderedObjects.Count} Structure Objects (calling IStObjSetupConfigurator.ConfigureDependentItem and IStObjSetupItemFactory.CreateDependentItem for each of them)." ) )
            {
                foreach( var r in orderedObjects )
                {
                    // Gets the StObjSetupDataBase that applies: the one of its base class or the one built from
                    // the attributes above if it is the root Ambient Contract.
                    Debug.Assert( r.Generalization == null || setupableItems.ContainsKey( r.Generalization ), "Generalizations are required: they are processed first." );

                    StObjSetupData generalizationData = null;
                    StObjSetupDataRootClass fromAbove;
                    if( r.Generalization != null ) fromAbove = generalizationData = setupableItems[r.Generalization];
                    else fromAbove = StObjSetupDataRootClass.CreateRootData( _monitor, r.ObjectType.GetTypeInfo().BaseType );

                    // Builds the StObjSetupData from the different attributes.
                    var data = new StObjSetupData( _monitor, r, fromAbove );
                    // Calls any attributes that is a IStObjSetupConfigurator with the StObjSetupData.
                    {
                        var all = data.StObj.Attributes.GetAllCustomAttributes<IStObjSetupConfigurator>();
                        foreach( IStObjSetupConfigurator c in all )
                        {
                            c.ConfigureDependentItem( _monitor, data );
                        }
                    }
                    // If the object itself is a IStObjSetupConfigurator, calls it.
                    IStObjSetupConfigurator objectItself = r.InitialObject as IStObjSetupConfigurator;
                    if( objectItself != null ) objectItself.ConfigureDependentItem( _monitor, data );

                    // Calls external configuration.
                    if( _configurator != null ) _configurator.ConfigureDependentItem( _monitor, data );

                    // Creates the IMutableDependentItem (or StObjDynamicPackageItem) configured with the StObjSetupData.
                    try
                    {
                        data.ResolveItemAndDriverTypes( _monitor );
                        if( _setupItemFactory != null ) data.SetupItem = _setupItemFactory.CreateSetupItem( _monitor, data );
                        if( data.SetupItem != null )
                        {
                            // An item has been created by the factory. 
                            // If the StObj has been declared as a Group or a Container: data.StObj.IsGroup == true, 
                            // we may check here that the type of the created item is able to be structurally considered as a Group (or a Container):
                            // we should just check here that it is a IDependentItemGroup/Container (we ignore the IDependentItemContainerTyped
                            // that may, later dynamically refuse to be a Container since this will be handled during ordering by The DependencySorter).
                            //
                            // The actual binding from the Items to their Container is handled below (once all items have been created).
                            // It is best to detect such inconsistencies below since we'll be able to give more precise error information to the user (ie, "This non Container Item 
                            // is referenced by that Item as a Container", instead of only "This non Container item is used as a Container.").
                        }
                        else
                        {
                            Type itemType = data.ItemType;
                            if( itemType == null )
                            {
                                StObjDynamicPackageItem stobjDynamicPackageItem = new StObjDynamicPackageItem( _monitor, data );
                                data.SetupItem = stobjDynamicPackageItem;
                            }
                            else
                            {
                                data.SetupItem = (IStObjSetupItem)_services.SimpleObjectCreate(_monitor, itemType, data );
                            }
                        }
                        // Configures Generalization since we got it above.
                        // Other properties (like dependencies) will be initialized later (once all setup items instances exist).
                        if( generalizationData != null )
                        {
                            data.SetupItem.Generalization = generalizationData.SetupItem.GetReference();
                        }
                        setupableItems.Add( r, data );
                    }
                    catch( Exception ex )
                    {
                        _monitor.Error( $"While initializing Setup item for StObj '{data.FullName}'.", ex );
                    }
                }
            }
        }

        void BindDependencies( Dictionary<IStObjResult, StObjSetupData> setupableItems )
        {
            using( _monitor.OpenInfo( "Binding dependencies between Setupable items." ) )
            {
                foreach( StObjSetupData data in setupableItems.Values )
                {
                    BindContainer( setupableItems, data );
                    foreach( IStObjResult req in data.StObj.Requires )
                    {
                        StObjSetupData reqD = setupableItems[req];
                        data.SetupItem.Requires.Add( reqD.SetupItem.GetReference() );
                    }
                    foreach( IStObjResult group in data.StObj.Groups )
                    {
                        StObjSetupData gData = setupableItems[group];
                        IMutableSetupItemGroup g = gData.SetupItem as IMutableSetupItemGroup;
                        if( g == null )
                        {
                            _monitor.Error( $"Structure Item '{data.FullName}' declares '{gData.FullName}' as a Group, but the latter is not a IMutableSetupItemGroup (only a IMutableSetupItem)." );
                        }
                        else
                        {
                            data.SetupItem.Groups.Add( g.GetReference() );
                        }
                    }
                    if( data.StObj.Children.Count > 0 )
                    {
                        // The StObj has children. 
                        IMutableSetupItemGroup g = data.SetupItem as IMutableSetupItemGroup;
                        if( g == null )
                        {
                            _monitor.Error( $"Structure Item '{data.FullName}' has associated children but it is not a IMutableSetupItemGroup (only a IMutableSetupItem)." );
                        }
                        else
                        {
                            foreach( IStObjResult child in data.StObj.Children )
                            {
                                StObjSetupData c = setupableItems[child];
                                g.Children.Add( c.SetupItem.GetReference() );
                            }
                        }
                    }
                }
            }
        }

        void BindContainer( Dictionary<IStObjResult, StObjSetupData> setupableItems, StObjSetupData data )
        {
            IStObjResult existingStObjContainer = data.StObj.ConfiguredContainer;
            StObjSetupData existing = existingStObjContainer != null ? setupableItems[existingStObjContainer] : null;
            if( existing != null )
            {
                if( data.ContainerFullName != null )
                {
                    if( existing.FullNameWithoutContext != data.ContainerFullName )
                    {
                        _monitor.Info( $"Container of '{data.FullName}' is '{existing.FullNameWithoutContext}'. (Original Container was: '{data.ContainerFullName}'.)" );
                    }
                    data.SetupItem.Container = new NamedDependentItemContainerRef( data.ContainerFullName );
                }
                IDependentItemContainer c = existing.SetupItem as IDependentItemContainer;
                if( c == null )
                {
                    if( existing.SetupItem != null )
                    {
                        _monitor.Error( $"Structure Item '{data.FullName}' is bound to a Container named '{existing.FullNameWithoutContext}' but the corresponding IDependentItem is not a IDependentItemContainer (its type is '{existing.SetupItem.GetType().FullName}')." );
                    }
                    else
                    {
                        _monitor.Error( $"Structure Item '{data.FullName}' is bound to a Container named '{existing.FullNameWithoutContext}' but the corresponding IDependentItem has not been successfully created." );
                    }
                }
                else
                {
                    data.SetupItem.Container = c.GetReference();
                }
            }
            else
            {
                // If the Container was not configured at the StObj level we let 
                // the ContainerFullName (if specified) be the "named reference".
                if( data.ContainerFullName != null )
                {
                    data.SetupItem.Container = new NamedDependentItemContainerRef( data.ContainerFullName );
                }
            }
        }

        class DynamicInitializerState : IStObjSetupDynamicInitializerState
        {
            readonly StObjSetupItemBuilder _builder;
            readonly IDictionary _memory;
            List<PushedAction> _actions;
            List<PushedAction> _nextRoundActions;
            int _currentRoundActions;

            class PushedAction
            {
                public PushedAction( IMutableSetupItem item, IStObjResult stObj, Action<IStObjSetupDynamicInitializerState, IMutableSetupItem, IStObjResult> action )
                {
                    Item = item;
                    StObj = stObj;
                    Action = action;
                }

                public readonly IMutableSetupItem Item;
                public readonly IStObjResult StObj;
                public readonly Action<IStObjSetupDynamicInitializerState, IMutableSetupItem, IStObjResult> Action;
            }

            internal DynamicInitializerState( StObjSetupItemBuilder builder )
            {
                _builder = builder;
                _memory = new Dictionary<object,object>();
                _actions = new List<PushedAction>();
            }

            public IActivityMonitor Monitor =>_builder._monitor; 

            public IServiceProvider ServiceProvider => _builder._services;

            public IDictionary Memory => _memory; 

            public int PushedActionsCount => _actions.Count; 

            public int PushedNextRoundActionsCount => _nextRoundActions != null ? _nextRoundActions.Count : 0; 

            public int CurrentRoundNumber => _currentRoundActions;
            
            public IMutableSetupItem CurrentItem;
            public IStObjResult CurrentStObj;

            public void PushAction( Action<IStObjSetupDynamicInitializerState, IMutableSetupItem, IStObjResult> a )
            {
                _actions.Add( new PushedAction( CurrentItem, CurrentStObj, a ) );
            }

            public void PushNextRoundAction( Action<IStObjSetupDynamicInitializerState, IMutableSetupItem, IStObjResult> a )
            {
                if( _nextRoundActions == null ) _nextRoundActions = new List<PushedAction>();
                _nextRoundActions.Add( new PushedAction( CurrentItem, CurrentStObj, a ) );
            }

            internal bool ExecuteActions()
            {
                for(;;)
                {
                    using( Monitor.OpenInfo( $"Starting intialization round n°{_currentRoundActions}." ) )
                    {
                        if( !ExecuteCurrentActions() ) return false;
                        if( _nextRoundActions == null ) return true;
                        Debug.Assert( _nextRoundActions.Count > 0 );
                        _actions = _nextRoundActions;
                        _nextRoundActions = null;
                        ++_currentRoundActions;
                    }
                }
            }

            bool ExecuteCurrentActions()
            {
                bool success = true;
                using( _builder._monitor.OnError( () => success = false ) )
                {
                    int i = 0;
                    while( i < _actions.Count && success )
                    {
                        PushedAction a = _actions[i];
                        try
                        {
                            CurrentItem = a.Item;
                            CurrentStObj = a.StObj;
                            a.Action( this, CurrentItem, CurrentStObj );
                        }
                        catch( Exception ex )
                        {
                            Monitor.Fatal( $"While calling a pushed action on '{CurrentItem.FullName}' (round n°{_currentRoundActions}).", ex );
                            Debug.Assert( success == false, "OnError dit the job..." );
                        }
                        ++i;
                    }
                }
                return success;
            }
        }

        bool CallDynamicInitializer( IReadOnlyList<IStObjResult> orderedObjects, Dictionary<IStObjResult, StObjSetupData> setupableItems )
        {
            using( _monitor.OpenInfo( "Dynamic initialization of Setup items." ) )
            {
                var state = new DynamicInitializerState( this );
                bool success = true;
                using( _monitor.OnError( () => success = false ) )
                {
                    foreach( IStObjResult o in orderedObjects )
                    {
                        IMutableSetupItem item = setupableItems[o].SetupItem;
                        state.CurrentItem = item;
                        state.CurrentStObj = o;
                        string initSource = null;
                        try
                        {
                            initSource = "Attributes";
                            // ApplyAttributesDynamicInitializer on attributes (attributes of the type itself come first).
                            {
                                var all = o.Attributes.GetAllCustomAttributes<IStObjSetupDynamicInitializer>();
                                foreach( IStObjSetupDynamicInitializer init in all )
                                {
                                    init.DynamicItemInitialize( state, item, o );
                                }
                            }
                            initSource = "Structured Item itself";
                            if( o.InitialObject is IStObjSetupDynamicInitializer objectItself ) objectItself.DynamicItemInitialize( state, item, o );
                            initSource = "Setup Item itself";
                            if( item is IStObjSetupDynamicInitializer itemItself ) itemItself.DynamicItemInitialize( state, item, o );
                            initSource = "Global StObjSetupBuilder initializer";
                            _dynamicInitializer?.DynamicItemInitialize( state, item, o );
                        }
                        catch( Exception ex )
                        {
                            _monitor.Error( $"While Dynamic item initialization (from {initSource}) of '{item.FullName}' for object '{o.ObjectType.FullName}'.", ex );
                            Debug.Assert( success == false, "OnError did the job..." );
                        }
                    }
                }
                // On success, we execute the pushed actions.
                if( success && (state.PushedActionsCount > 0 || state.PushedNextRoundActionsCount > 0) )
                {
                    using( _monitor.OpenInfo( $"Executing {state.PushedActionsCount} deferred actions." ) )
                    {
                        success = state.ExecuteActions();
                    }
                }
                return success;
            }
        }
        
        #endregion

    }
}
