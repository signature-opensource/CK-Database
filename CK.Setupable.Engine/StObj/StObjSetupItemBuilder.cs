using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class StObjSetupItemBuilder
    {
        readonly IActivityLogger _logger;
        readonly IStObjSetupConfigurator _configurator;
        readonly IStObjSetupItemFactory _setupItemFactory;
        readonly IStObjSetupDynamicInitializer _dynamicInitializer;

        public StObjSetupItemBuilder( IActivityLogger logger, IStObjSetupConfigurator configurator = null, IStObjSetupItemFactory setupItemFactory = null, IStObjSetupDynamicInitializer dynamicInitializer = null )
        {
            if( logger == null ) throw new ArgumentNullException( "_logger" );
            _logger = logger;
            _configurator = configurator;
            _setupItemFactory = setupItemFactory;
            _dynamicInitializer = dynamicInitializer;
        }

        /// <summary>
        /// Initializes a set of <see cref="ISetupItem"/> given a dependency-ordered list of <see cref="IStObjRuntime"/> objects.
        /// </summary>
        /// <param name="rootObjects">Root <see cref="IStObjRuntime"/> objects.</param>
        /// <returns>A set of setup items.</returns>
        public IEnumerable<ISetupItem> Build( IReadOnlyList<IStObjRuntime> orderedObjects )
        {
            if( orderedObjects == null ) throw new ArgumentNullException( "rootObjects" );

            var setupableItems = new Dictionary<IStObjRuntime, StObjSetupData>();
            BuildSetupItems( orderedObjects, setupableItems );
            BindDependencies( setupableItems );
            CallDynamicInitializer( orderedObjects, setupableItems );
            return setupableItems.Values.Select( data => data.SetupItem );
        }

        void BuildSetupItems( IReadOnlyList<IStObjRuntime> orderedObjects, Dictionary<IStObjRuntime, StObjSetupData> setupableItems )
        {
            using( _logger.OpenGroup( LogLevel.Info, "Building setupable items from {0} Structure Objects.", orderedObjects.Count ) )
            {
                foreach( var r in orderedObjects )
                {
                    // Gets the StObjSetupDataBase that applies: the one of its base class or the one built from
                    // the attibutes above if it is the root Ambient Contract.
                    Debug.Assert( r.Generalization == null || setupableItems.ContainsKey( r.Generalization ), "Generalizations are required: they are processed first." );

                    StObjSetupData generalizationData = null;
                    StObjSetupDataBase fromAbove;
                    if( r.Generalization != null ) fromAbove = generalizationData = setupableItems[r.Generalization];
                    else fromAbove = StObjSetupDataBase.CreateRootData( _logger, r.ObjectType.BaseType );

                    // Builds the StObjSetupData from the different attributes.
                    var data = new StObjSetupData( _logger, r, fromAbove );
                    // Calls any attributes that is a IStObjSetupConfigurator with the StObjSetupData.
                    // ApplyAttributesConfigurator
                    {
                        var all = data.StObj.Attributes.GetAllCustomAttributes<IStObjSetupConfigurator>();
                        foreach( IStObjSetupConfigurator c in all )
                        {
                            c.ConfigureDependentItem( _logger, data );
                        }
                    }
                    // If the object itself is a IStObjSetupConfigurator, calls it.
                    IStObjSetupConfigurator objectItself = r.Object as IStObjSetupConfigurator;
                    if( objectItself != null ) objectItself.ConfigureDependentItem( _logger, data );

                    // Calls external configuration.
                    if( _configurator != null ) _configurator.ConfigureDependentItem( _logger, data );

                    // Creates the IMutableDependentItem (or StObjDynamicPackageItem) configured with the StObjSetupData.
                    try
                    {
                        data.ResolveItemAndDriverTypes( _logger );
                        if( _setupItemFactory != null ) data.SetupItem = _setupItemFactory.CreateDependentItem( _logger, data );
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
                            if( itemType == null ) data.SetupItem = new StObjDynamicPackageItem( _logger, data );
                            else
                            {
                                data.SetupItem = (IMutableSetupItem)Activator.CreateInstance( itemType, _logger, data );
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
                        _logger.Error( ex, "While initializing Setup item for StObj '{0}'.", data.FullName );
                    }
                }
            }
        }

        void BindDependencies( Dictionary<IStObjRuntime, StObjSetupData> setupableItems )
        {
            using( _logger.OpenGroup( LogLevel.Info, "Binding dependencies." ) )
            {
                foreach( StObjSetupData data in setupableItems.Values )
                {
                    BindContainer( setupableItems, data );
                    foreach( IStObjRuntime req in data.StObj.Requires )
                    {
                        StObjSetupData reqD = setupableItems[req];
                        data.SetupItem.Requires.Add( reqD.SetupItem.GetReference() );
                    }
                    foreach( IStObjRuntime group in data.StObj.Groups )
                    {
                        StObjSetupData gData = setupableItems[group];
                        IMutableSetupItemGroup g = gData.SetupItem as IMutableSetupItemGroup;
                        if( g == null )
                        {
                            _logger.Error( "Structure Item '{0}' declares '{1}' as a Group, but the latter is not a IMutableSetupItemGroup (only a IMutableSetupItem).", data.FullName, gData.FullName );
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
                            _logger.Error( "Structure Item '{0}' has associated children but it is not a IMutableSetupItemGroup (only a IMutableSetupItem).", data.FullName );
                        }
                        else
                        {
                            foreach( IStObjRuntime child in data.StObj.Children )
                            {
                                StObjSetupData c = setupableItems[child];
                                g.Children.Add( c.SetupItem.GetReference() );
                            }
                        }
                    }
                }
            }
        }

        void BindContainer( Dictionary<IStObjRuntime, StObjSetupData> setupableItems, StObjSetupData data )
        {
            IStObjRuntime existingStObjContainer = data.StObj.ConfiguredContainer;
            StObjSetupData existing = existingStObjContainer != null ? setupableItems[existingStObjContainer] : null;
            if( existing != null )
            {
                if( data.ContainerFullName != null )
                {
                    if( existing.FullNameWithoutContext != data.ContainerFullName )
                    {
                        _logger.Error( "Structure Item '{0}' is bound to Container named '{1}' but the PackageAttribute states that it must be in '{2}'.", data.FullName, existing.FullNameWithoutContext, data.ContainerFullName );
                    }
                    // Even when a mismatch exists, we continue and bind the container configred at the StObj level (trying to raise more errors).
                }
                IDependentItemContainer c = existing.SetupItem as IDependentItemContainer;
                if( c == null )
                {
                    if( existing.SetupItem != null )
                    {
                        _logger.Error( "Structure Item '{0}' is bound to a Container named '{1}' but the corresponding IDependentItem is not a IDependentItemContainer (its type is '{2}').", data.FullName, existing.FullNameWithoutContext, existing.SetupItem.GetType().FullName );
                    }
                    else
                    {
                        _logger.Error( "Structure Item '{0}' is bound to a Container named '{1}' but the corresponding IDependentItem has not been successfully created.", data.FullName, existing.FullNameWithoutContext );
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

        void CallDynamicInitializer( IReadOnlyList<IStObjRuntime> orderedObjects, Dictionary<IStObjRuntime, StObjSetupData> setupableItems )
        {
            foreach( var o in orderedObjects )
            {
                string initSource = null;
                IMutableSetupItem item = setupableItems[o].SetupItem;
                try
                {
                    initSource = "Attributes";
                    // ApplyAttributesDynamicInitializer
                    {
                        var all = o.Attributes.GetAllCustomAttributes<IStObjSetupDynamicInitializer>();
                        foreach( IStObjSetupDynamicInitializer init in all )
                        {
                            init.DynamicItemInitialize( _logger, item, o );
                        }
                    }
                    initSource = "Structured Item itself";
                    if( o.Object is IStObjSetupDynamicInitializer ) ((IStObjSetupDynamicInitializer)o.Object).DynamicItemInitialize( _logger, item, o );
                    initSource = "Setup Item itself";
                    if( item is IStObjSetupDynamicInitializer ) ((IStObjSetupDynamicInitializer)item).DynamicItemInitialize( _logger, item, o );
                    initSource = "global StObjSetupBuilder initializer";
                    if( _dynamicInitializer != null ) _dynamicInitializer.DynamicItemInitialize( _logger, item, o );
                }
                catch( Exception ex )
                {
                    _logger.Error( ex, "While Dynamic item initialization (from {2}) of '{0}' for object '{1}'.", item.FullName, o.ObjectType.Name, initSource );
                }
            }
        }

    }
}
