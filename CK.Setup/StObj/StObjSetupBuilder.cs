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

        public StObjSetupBuilder( IActivityLogger logger, IStObjSetupConfigurator configurator = null )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            _logger = logger;
            _configurator = configurator;
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
                    // the attibutes above if it is the root ambiant contract.
                    Debug.Assert( r.Generalization == null || setupableItems.ContainsKey( r.Generalization ), "Generalizations are required: they are processed first." );

                    StObjSetupDataBase fromFather;
                    if( r.Generalization != null ) fromFather = setupableItems[r.Generalization];
                    else fromFather = StObjSetupDataBase.CreateRootData( _logger, r.ObjectType.BaseType );
                    
                    // Builds the StObjSetupData from the different attributes.
                    var data = new StObjSetupData( _logger, r, fromFather );
                    // Calls any attributes that is a IStObjSetupConfigurator with the StObjSetupData.
                    SetupAttribute.ApplyAttributesConfigurator( _logger, r.ObjectType, data );

                    // If the object itself is a IStObjSetupConfigurator, calls it.
                    IStObjSetupConfigurator objectItself = r.StructuredObject as IStObjSetupConfigurator;
                    if( objectItself != null ) objectItself.ConfigureDependentItem( _logger, data );

                    // Calls external configuration.
                    if( _configurator != null ) _configurator.ConfigureDependentItem( _logger, data );
                    data.CreateSetupItem();
                    setupableItems.Add( r, data );
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
                        data.SetupItem.Requires.Add( reqD.SetupItem );
                    }
                    foreach( IStObj reqBy in data.StObj.RequiredBy )
                    {
                        StObjSetupData reqDBy = setupableItems[reqBy];
                        data.SetupItem.RequiredBy.Add( reqDBy.SetupItem );
                    }
                }
            }
            return setupableItems.Values.Select( data => data.SetupItem );
        }

        void BindContainer( Dictionary<IStObj, StObjSetupData> setupableItems, StObjSetupData data )
        {
            IStObj existingStObjContainer = data.StObj.Container;
            StObjSetupData existing = existingStObjContainer != null ? setupableItems[existingStObjContainer] : null;
            if( existing != null )
            {
                if( data.ContainerFullName != null )
                {
                    if( existing.FullNameWithoutContext != data.ContainerFullName )
                    {
                        _logger.Error( "Structure Object '{0}' is bound to Container named '{1}' but the PackageAttribute states that it must be in '{2}'.", data.FullName, existing.FullNameWithoutContext, data.ContainerFullName );
                    }
                }
                data.SetupItem.Container = existing.SetupItem;
            }
            else
            {
                if( data.ContainerFullName != null )
                {
                    data.SetupItem.Container = new NamedDependentItemContainerRef( data.ContainerFullName );
                }
            }
        }
    }
}
