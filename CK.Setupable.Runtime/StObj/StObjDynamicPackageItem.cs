using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;
using System.Reflection;

namespace CK.Setup
{
    /// <summary>
    /// Default <see cref="IMutableSetupItemContainer"/> implementation associated to <see cref="IStObjResult"/> object.
    /// Used when the <see cref="IStObjSetupData"/> does not specify a dedicated implementation (<see cref="IStObjSetupData.ItemType"/> 
    /// nor <see cref="IStObjSetupData.ItemTypeName"/> are set).
    /// This class can (and should) be used as a base class for more specific item implementation.
    /// </summary>
    public class StObjDynamicPackageItem : DynamicPackageItem, IStObjSetupItem, ISetupObjectItem
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
        /// Gets the associated object instance (the final, most specialized, structured object). 
        /// </summary>
        public object ActualObject => _stObj.InitialObject; 

    }
}
