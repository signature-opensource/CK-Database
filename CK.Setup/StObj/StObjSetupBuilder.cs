using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class StObjSetupBuilder
    {
        readonly IActivityLogger _logger;
        readonly IStObjSetupConfigurator _configurator;
        readonly IStObjSetupItemFactory _setupItemFactory;

        public StObjSetupBuilder( IActivityLogger logger, IStObjSetupConfigurator configurator = null, IStObjSetupItemFactory setupItemFactory = null )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            _logger = logger;
            _configurator = configurator;
            _setupItemFactory = setupItemFactory;
        }

        /// <summary>
        /// Initializes a set of <see cref="IDependentItem"/> given a dependency-ordered list of <see cref="IStObj"/> objects.
        /// </summary>
        /// <param name="rootObjects">Root <see cref="IStObj"/> objects.</param>
        /// <returns>A set of dependent items.</returns>
        public IEnumerable<IDependentItem> Build( IReadOnlyList<IStObj> orderedObjects )
        {
            if( orderedObjects == null ) throw new ArgumentNullException( "rootObjects" );

            var setupableItems = new Dictionary<IStObj, StObjSetupData>();
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
                    SetupAttribute.ApplyAttributesConfigurator( _logger, r.ObjectType, data );

                    // If the object itself is a IStObjSetupConfigurator, calls it.
                    IStObjSetupConfigurator objectItself = r.Object as IStObjSetupConfigurator;
                    if( objectItself != null ) objectItself.ConfigureDependentItem( _logger, data );

                    // Calls external configuration.
                    if( _configurator != null ) _configurator.ConfigureDependentItem( _logger, data );

                    // Creates the IMutableDependentItem (or StObjDynamicPackageItem) configured with the StObjSetupData.
                    try
                    {
                        data.ResolveTypes( _logger );
                        if( _setupItemFactory != null ) data.SetupItem = _setupItemFactory.CreateDependentItem( _logger, data );
                        if( data.SetupItem != null )
                        {
                            // An item has been created by the factory. 
                            // If the StObj has been referenced as a Container: data.StObj.IsGroup == true, 
                            // we may check here that the type of the created item is able to be structurally considered as a Container:
                            // we should just check here that it is a IDependentItemContainer (we ignore the IDependentItemContainerAsk 
                            // that may, later dynamically refuses to be a Container since this will be handled during ordering by The DependencySorter).
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
                                data.SetupItem = (IMutableDependentItem)Activator.CreateInstance( itemType, _logger, data );
                            }
                        }
                        // Configures Generalization since we got it above.
                        // Other properties (like dependencies) will be initialized later (once all setup items instances exist).
                        if( generalizationData != null )
                        {
                            data.SetupItem.Generalization = generalizationData.SetupItem.GetReference();
                            ISetupItemAwareObject awareObject = data.StObj.Object as ISetupItemAwareObject;
                            if( awareObject != null ) awareObject.SetupItem = data.SetupItem;
                        }
                        setupableItems.Add( r, data );
                    }
                    catch( Exception ex )
                    {
                        _logger.Error( ex, "While initializing Setup item for StObj '{0}'.", data.FullName );
                    }
                }
            }
            using( _logger.OpenGroup( LogLevel.Info, "Binding dependencies." ) )
            {
                foreach( StObjSetupData data in setupableItems.Values )
                {
                    BindContainer( setupableItems, data );
                    foreach( IStObj req in data.StObj.Requires )
                    {
                        StObjSetupData reqD = setupableItems[req];
                        data.SetupItem.Requires.Add( reqD.SetupItem.GetReference() );
                    }
                }
            }
            return setupableItems.Values.Select( data => data.SetupItem );
        }

        void BindContainer( Dictionary<IStObj, StObjSetupData> setupableItems, StObjSetupData data )
        {
            IStObj existingStObjContainer = data.StObj.ConfiguredContainer;
            StObjSetupData existing = existingStObjContainer != null ? setupableItems[existingStObjContainer] : null;
            if( existing != null )
            {
                if( data.ContainerFullName != null )
                {
                    if( existing.FullNameWithoutContext != data.ContainerFullName )
                    {
                        _logger.Error( "Structure Object '{0}' is bound to Container named '{1}' but the PackageAttribute states that it must be in '{2}'.", data.FullName, existing.FullNameWithoutContext, data.ContainerFullName );
                    }
                    // Even when a mismatch exists, we continue and bind the container configred at the StObj level (trying to raise more errors).
                }
                IDependentItemContainer c = existing.SetupItem as IDependentItemContainer;
                if( c == null )
                {
                    if( existing.SetupItem != null )
                    {
                        _logger.Error( "Structure Object '{0}' is bound to a Container named '{1}' but the corresponding IDependentItem is not a IDependentItemContainer (its type is '{2}').", data.FullName, existing.FullNameWithoutContext, existing.SetupItem.GetType().FullName );
                    }
                    else
                    {
                        _logger.Error( "Structure Object '{0}' is bound to a Container named '{1}' but the corresponding IDependentItem has not been successfully created.", data.FullName, existing.FullNameWithoutContext );
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
    }
}
