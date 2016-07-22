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
    public class StObjDynamicPackageItem : DynamicPackageItem, IStObjSetupItem
    {
        readonly IStObjResult _stObj;

        /// <summary>
        /// Initializes a new <see cref="StObjDynamicPackageItem"/> initialized by a <see cref="IStObjSetupData"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Descriptive data that is used to configure this item.</param>
        public StObjDynamicPackageItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( data.StObj.ItemKind == DependentItemKindSpec.Item ? "StObjItem" : "StObjPackage", (object)data.DriverType ?? data.DriverTypeName )
        {
            Debug.Assert( Model == null, "Initially, a DynamicPackageItem has no Model." );
            Debug.Assert( ObjectsPackage == null, "Initially, a DynamicPackageItem has no ObjectsPackage." );
            Debug.Assert( data.ItemType == null || typeof( StObjDynamicPackageItem ).IsAssignableFrom( data.ItemType ), "If we are using a StObjDynamicPackageItem, this is because no explicit ItemType (nor ItemTypeName) have been set, or it is a type that specializes this." );
            ItemKind = (DependentItemKind)data.StObj.ItemKind;
            SetVersionsString( data.Versions );
            _stObj = data.StObj;
            FullName = data.FullName;
            Requires.AddRange( data.Requires );
            RequiredBy.AddRange( data.RequiredBy );
            Groups.AddRange( data.Groups );
            Children.AddRange( data.Children );
        }

        /// <summary>
        /// Gets the StObj.
        /// </summary>
        public IStObjResult StObj => _stObj; 
        
        /// <summary>
        /// Gets the associated object instance (the final, most specialized, structured object) when this is bound to a StObj (<see cref="StObj"/> is not null). 
        /// Otherwise gets the object associated explicitely when this setup item has been created.
        /// See remarks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The function that is injected during the graph creation (at the StObj level) simply returns the <see cref="IStObjResult.InitialObject"/> instance that is NOT always a "real",
        /// fully operational, object since its auto implemented methods (or other aspects) have not been generated yet.
        /// </para>
        /// <para>
        /// Once the final assembly has been generated, this function is updated with <see cref="IContextualStObjMap.Obtain"/>: during the setup phasis, the actual 
        /// objects that are associated to items are "real" objects produced/managed by the final <see cref="StObjContextRoot"/>.
        /// </para>
        /// <para>
        /// In order to honor potential transient lifetime (one day), these object should not be aggressively cached, this is why this is a <see cref="GetObject()"/> function 
        /// and not a simple 'Object' or 'FinalObject' property. 
        /// </para>
        /// </remarks>
        public object GetObject() => _stObj.ObjectAccessor(); 

    }
}
