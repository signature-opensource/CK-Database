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
    /// nor <see cref="IStObjSetupData.ItemTypeName"/> are set).
    /// This class can (and should) be used as a base class for more specific item implementation.
    /// </summary>
    public class StObjDynamicPackageItem : DynamicPackageItem, IMutableSetupItem
    {
        Func<object> _objectAccessor;

        /// <summary>
        /// Initializes a new <see cref="StObjDynamicPackageItem"/> that must be manually configured.
        /// </summary>
        /// <param name="itemType">
        /// Type of item (must not be longer than 16 characters). 
        /// It is "StObjItem" or "StObjPackage" when initialized by the <see cref="StObjDynamicPackageItem(IActivityMonitor,IStObjSetupData)">other constructor</see>.
        /// </param>
        /// <param name="driverType">Type of the associated driver or its assembly qualified name.</param>
        /// <param name="objectAccessor">A function that knows how to obtain the final, most specialized, structured object.</param>
        protected StObjDynamicPackageItem( string itemType, object driverType, Func<object> objectAccessor )
            : base( itemType, driverType )
        {
            if( objectAccessor == null ) throw new ArgumentNullException( "obj" );
            _objectAccessor = objectAccessor;
        }

        /// <summary>
        /// Initializes a new <see cref="StObjDynamicPackageItem"/> initialized by a <see cref="IStObjSetupData"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Descriptive data that is used to configure this item.</param>
        public StObjDynamicPackageItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( data.StObj.ItemKind == DependentItemKindSpec.Item ? "StObjItem" : "StObjPackage", (object)data.DriverType ?? data.DriverTypeName )
        {
            Debug.Assert( Model == null, "Initially, a DynamicPackageItem has no model." );
            Debug.Assert( data.ItemType == null || typeof( StObjDynamicPackageItem ).IsAssignableFrom( data.ItemType ), "If we are using a StObjDynamicPackageItem, this is because no explicit ItemType (nor ItemTypeName) have been set, or it is a type that specializes this." );
            ItemKind = (DependentItemKind)data.StObj.ItemKind;
            SetVersionsString( data.Versions );
            _objectAccessor = data.StObj.ObjectAccessor;
        }

        /// <summary>
        /// Gets the associated object instance (the final, most specialized, structured object).
        /// See remarks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The function that is injected during the graph creation (at the StObj level) simply returns the <see cref="IStObjResult.StObj"/> instance that is NOT always a "real",
        /// fully operational, object since its auto implemented methods (or other aspects) have not been generated yet.
        /// </para>
        /// <para>
        /// Once the final assembly has been generated, this function is updated with <see cref="IContextualStObjMap.Obtain"/>: during the setup phasis, the actual 
        /// objects that are associated to items are "real" objects produced/managed by the final <see cref="StObjContextRoot"/>.
        /// </para>
        /// <para>
        /// In order to honor potential transient lifetime, these object should not be aggressively cached, this is why this is a <see cref="GetObject()"/> function 
        /// and not a simple Object or FinalObject property. 
        /// </para>
        /// </remarks>
        public object GetObject() 
        { 
            return _objectAccessor(); 
        }
    }
}
