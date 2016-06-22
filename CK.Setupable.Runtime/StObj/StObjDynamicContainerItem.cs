#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\StObj\StObjDynamicPackageItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
    public class StObjDynamicContainerItem : DynamicContainerItem, IStObjSetupItem
    {
        readonly IStObjResult _stObj;

        /// <summary>
        /// Initializes a new <see cref="StObjDynamicContainerItem"/> initialized by a <see cref="IStObjSetupData"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Descriptive data that is used to configure this item.</param>
        /// <param name="defaultDriverType">If <see cref="IStObjSetupData.DriverType"/> and <see cref="IStObjSetupData.DriverTypeName"/> is null, this driver is used.</param>
        public StObjDynamicContainerItem( IActivityMonitor monitor, IStObjSetupData data, Type defaultDriverType = null )
            : base( (object)data.DriverType ?? (object)data.DriverTypeName ?? defaultDriverType )
        {
            Debug.Assert( data.ItemType == null || typeof( StObjDynamicContainerItem ).IsAssignableFrom( data.ItemType ), "If we are using a StObjDynamicContainerItem, this is because no explicit ItemType (nor ItemTypeName) have been set, or it is a type that specializes this." );
            ItemKind = (DependentItemKind)data.StObj.ItemKind;
            _stObj = data.StObj;
            FullName = data.FullName;
            Requires.AddRange( data.Requires );
            RequiredBy.AddRange( data.RequiredBy );
            Groups.AddRange( data.Groups );
            Children.AddRange( data.Children );
        }

        /// <summary>
        /// Gets the StObj. Null if this item is directly bound to an object.
        /// </summary>
        public IStObjResult StObj => _stObj; 
        
        /// <summary>
        /// Gets the associated object instance (the final, most specialized, structured object).
        /// See remarks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The function that is injected during the graph creation (at the StObj level) simply returns the <see cref="IStObjResult.InitialObject"/> instance that is NOT always a "real",
        /// fully operational, object since its auto implemented methods (or other aspects) have not been generated yet.
        /// </para>
        /// <para>
        /// In order to honor potential transient lifetime (one day), these object should not be aggressively cached, this is why this is a <see cref="GetObject()"/> function 
        /// and not a simple 'Object' or 'FinalObject' property. 
        /// </para>
        /// </remarks>
        public object GetObject()  => _stObj.ObjectAccessor(); 

    }
}
