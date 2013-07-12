using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Default <see cref="IMutableSetupItemContainer"/> implementation associated to <see cref="IStObjResult"/> object.
    /// Used when the <see cref="IStObjSetupData"/> does not specify a dedicated implementation (<see cref="IStObjSetupData.ItemType"/> 
    /// nor <see cref="IStObjSetupData.ItemTypeName"/> are set) but can also be used as a base class for more specific item implementation.
    /// </summary>
    public class StObjDynamicPackageItem : DynamicPackageItem
    {
        /// <summary>
        /// Initializes a new <see cref="StObjDynamicPackageItem"/> that must be manually configured.
        /// </summary>
        /// <param name="itemType">
        /// Type of item (must not be longer than 16 characters). 
        /// It is "StObjItem" or "StObjPackage" when initialized by the <see cref="StObjDynamicPackageItem(IActivityLogger,IStObjSetupData)">other constructor</see>.
        /// </param>
        /// <param name="driverType">Type of the associated driver or its assembly qualified name.</param>
        /// <param name="obj">The final <see cref="Object"/>.</param>
        protected StObjDynamicPackageItem( string itemType, object driverType, object obj )
            : base( itemType, driverType )
        {
            if( obj == null ) throw new ArgumentNullException( "obj" );
            Object = obj;
        }

        /// <summary>
        /// Initializes a new <see cref="StObjDynamicPackageItem"/> initialized by a <see cref="IStObjSetupData"/>.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="data">Descriptive data that is used to configure this item.</param>
        public StObjDynamicPackageItem( IActivityLogger logger, IStObjSetupData data )
            : base( data.StObj.ItemKind == DependentItemKindSpec.Item ? "StObjItem" : "StObjPackage", (object)data.DriverType ?? data.DriverTypeName )
        {
            Debug.Assert( Model == null, "Initially, a DynamicPackageItem has no model." );
            Debug.Assert( data.ItemType == null || typeof( StObjDynamicPackageItem ).IsAssignableFrom( data.ItemType ), "If we are using a StObjDynamicPackageItem, this is because no explicit ItemType (nor ItemTypeName) have been set, or it is a type that specializes this." );
            ItemKind = (DependentItemKind)data.StObj.ItemKind;
            SetVersionsString( data.Versions );
            Object = data.StObj.Object;
        }

        /// <summary>
        /// Gets the associated object instance (the final, most specialized, structured object).
        /// </summary>
        public object Object { get; private set; }

    }
}
