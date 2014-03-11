using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Collections;

namespace CK.Setup
{
    public class StObjSetupItemBuilder
    {
        readonly IActivityMonitor _monitor;
        readonly IStObjSetupConfigurator _configurator;
        readonly IStObjSetupItemFactory _setupItemFactory;
        readonly IStObjSetupDynamicInitializer _dynamicInitializer;

        public StObjSetupItemBuilder( IActivityMonitor monitor, IStObjSetupConfigurator configurator = null, IStObjSetupItemFactory setupItemFactory = null, IStObjSetupDynamicInitializer dynamicInitializer = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "_monitor" );
            _monitor = monitor;
            _configurator = configurator;
            _setupItemFactory = setupItemFactory;
            _dynamicInitializer = dynamicInitializer;
        }

        /// <summary>
        /// Initializes a set of <see cref="ISetupItem"/> given a dependency-ordered list of <see cref="IStObjResult"/> objects.
        /// </summary>
        /// <param name="rootObjects">Root <see cref="IStObjResult"/> objects.</param>
        /// <returns>A set of setup items.</returns>
        public IEnumerable<ISetupItem> Build( IReadOnlyList<IStObjResult> orderedObjects )
        {
            if( orderedObjects == null ) throw new ArgumentNullException( "rootObjects" );

            var setupableItems = new Dictionary<IStObjResult, StObjSetupData>();
            BuildSetupItems( orderedObjects, setupableItems );
            BindDependencies( setupableItems );
            if( !CallDynamicInitializer( orderedObjects, setupableItems ) ) return null;
            return setupableItems.Values.Select( data => data.SetupItem );
        }

        void BuildSetupItems( IReadOnlyList<IStObjResult> orderedObjects, Dictionary<IStObjResult, StObjSetupData> setupableItems )
        {
            using( _monitor.OpenInfo().Send( "Building setupable items from {0} Structure Objects (calling IStObjSetupConfigurator.ConfigureDependentItem and IStObjSetupItemFactory.CreateDependentItem for each of them).", orderedObjects.Count ) )
            {
                foreach( var r in orderedObjects )
                {
                    // Gets the StObjSetupDataBase that applies: the one of its base class or the one built from
                    // the attributes above if it is the root Ambient Contract.
                    Debug.Assert( r.Generalization == null || setupableItems.ContainsKey( r.Generalization ), "Generalizations are required: they are processed first." );

                    StObjSetupData generalizationData = null;
                    StObjSetupDataBase fromAbove;
                    if( r.Generalization != null ) fromAbove = generalizationData = setupableItems[r.Generalization];
                    else fromAbove = StObjSetupDataBase.CreateRootData( _monitor, r.ObjectType.BaseType );

                    // Builds the StObjSetupData from the different attributes.
                    var data = new StObjSetupData( _monitor, r, fromAbove );
                    // Calls any attributes that is a IStObjSetupConfigurator with the StObjSetupData.
                    // ApplyAttributesConfigurator
                    {
                        var all = data.StObj.Attributes.GetAllCustomAttributes<IStObjSetupConfigurator>();
                        foreach( IStObjSetupConfigurator c in all )
                        {
                            c.ConfigureDependentItem( _monitor, data );
                        }
                    }
                    // If the object itself is a IStObjSetupConfigurator, calls it.
                    IStObjSetupConfigurator objectItself = r.Object as IStObjSetupConfigurator;
                    if( objectItself != null ) objectItself.ConfigureDependentItem( _monitor, data );

                    // Calls external configuration.
                    if( _configurator != null ) _configurator.ConfigureDependentItem( _monitor, data );

                    // Creates the IMutableDependentItem (or StObjDynamicPackageItem) configured with the StObjSetupData.
                    try
                    {
                        data.ResolveItemAndDriverTypes( _monitor );
                        if( _setupItemFactory != null ) data.SetupItem = _setupItemFactory.CreateDependentItem( _monitor, data );
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
                                stobjDynamicPackageItem.FullName = data.FullName;
                                data.SetupItem = stobjDynamicPackageItem;
                            }
                            else
                            {
                                data.SetupItem = (IMutableSetupItem)Activator.CreateInstance( itemType, _monitor, data );
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
                        _monitor.Error().Send( ex, "While initializing Setup item for StObj '{0}'.", data.FullName );
                    }
                }
            }
        }

        void BindDependencies( Dictionary<IStObjResult, StObjSetupData> setupableItems )
        {
            using( _monitor.OpenInfo().Send( "Binding dependencies between Setupable items." ) )
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
                            _monitor.Error().Send( "Structure Item '{0}' declares '{1}' as a Group, but the latter is not a IMutableSetupItemGroup (only a IMutableSetupItem).", data.FullName, gData.FullName );
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
                            _monitor.Error().Send( "Structure Item '{0}' has associated children but it is not a IMutableSetupItemGroup (only a IMutableSetupItem).", data.FullName );
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
                        _monitor.Error().Send( "Structure Item '{0}' is bound to Container named '{1}' but the PackageAttribute states that it must be in '{2}'.", data.FullName, existing.FullNameWithoutContext, data.ContainerFullName );
                    }
                    // Even when a mismatch exists, we continue and bind the container configred at the StObj level (trying to raise more errors).
                }
                IDependentItemContainer c = existing.SetupItem as IDependentItemContainer;
                if( c == null )
                {
                    if( existing.SetupItem != null )
                    {
                        _monitor.Error().Send( "Structure Item '{0}' is bound to a Container named '{1}' but the corresponding IDependentItem is not a IDependentItemContainer (its type is '{2}').", data.FullName, existing.FullNameWithoutContext, existing.SetupItem.GetType().FullName );
                    }
                    else
                    {
                        _monitor.Error().Send( "Structure Item '{0}' is bound to a Container named '{1}' but the corresponding IDependentItem has not been successfully created.", data.FullName, existing.FullNameWithoutContext );
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
            readonly List<PushedAction> _actions;

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
                _memory = new Hashtable();
                _actions = new List<PushedAction>();
            }

            public IActivityMonitor Monitor { get { return _builder._monitor; } }

            public IDictionary Memory { get { return _memory; } }

            public int PushedActionsCount { get { return _actions.Count; } }
            
            public IMutableSetupItem CurrentItem;
            public IStObjResult CurrentStObj;

            public void PushAction( Action<IStObjSetupDynamicInitializerState, IMutableSetupItem, IStObjResult> a )
            {
                _actions.Add( new PushedAction( CurrentItem, CurrentStObj, a ) );
            }

            internal bool ExecuteActions()
            {
                bool success = true;
                int i = 0;
                while( i < _actions.Count )
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
                        Monitor.Error().Send( ex, "While calling a pushed action on '{0}'.", CurrentItem.FullName );
                        success = false;
                    }
                    ++i;
                }
                return success;
            }
        }

        bool CallDynamicInitializer( IReadOnlyList<IStObjResult> orderedObjects, Dictionary<IStObjResult, StObjSetupData> setupableItems )
        {
            using( _monitor.OpenInfo().Send( "Dynamic initialization of Setup items (calling IStObjSetupDynamicInitializer.DynamicItemInitialize for each of them)." ) )
            {
                var state = new DynamicInitializerState( this );
                bool success = true;
                foreach( var o in orderedObjects )
                {
                    IMutableSetupItem item = setupableItems[o].SetupItem;
                    state.CurrentItem = item;
                    state.CurrentStObj = o;
                    string initSource = null;
                    try
                    {
                        initSource = "Attributes";
                        // ApplyAttributesDynamicInitializer
                        {
                            var all = o.Attributes.GetAllCustomAttributes<IStObjSetupDynamicInitializer>();
                            foreach( IStObjSetupDynamicInitializer init in all )
                            {
                                init.DynamicItemInitialize( state, item, o );
                            }
                        }
                        initSource = "Structured Item itself";
                        if( o.Object is IStObjSetupDynamicInitializer ) ((IStObjSetupDynamicInitializer)o.Object).DynamicItemInitialize( state, item, o );
                        initSource = "Setup Item itself";
                        if( item is IStObjSetupDynamicInitializer ) ((IStObjSetupDynamicInitializer)item).DynamicItemInitialize( state, item, o );
                        initSource = "global StObjSetupBuilder initializer";
                        if( _dynamicInitializer != null ) _dynamicInitializer.DynamicItemInitialize( state, item, o );
                    }
                    catch( Exception ex )
                    {
                        _monitor.Error().Send( ex, "While Dynamic item initialization (from {2}) of '{0}' for object '{1}'.", item.FullName, o.ObjectType.Name, initSource );
                        success = false;
                    }
                }
                // On success, we execute the pushed actions.
                if( success && state.PushedActionsCount > 0 )
                {
                    using( _monitor.OpenInfo().Send( "Executing {0} deferred actions.", state.PushedActionsCount ) )
                    {
                        success = state.ExecuteActions();
                    }
                }
                return success;
            }
        }

    }
}
