#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\IMutableSetupItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A mutable version of an <see cref="ISetupItem"/> with only object support (generalization is not 
    /// supported at this level).
    /// The specialized <see cref="IMutableSetupItem"/> introduces <see cref="IMutableSetupItem.ItemKind"/> 
    /// and <see cref="IMutableSetupItem.Generalization"/> and <see cref="IMutableSetupItemGroup"/> offers 
    /// children collection.
    /// Its <see cref="IContextLocNaming.Context">Context</see>, <see cref="IContextLocNaming.Location">Location</see>, <see cref="IContextLocNaming.Name">Name</see> 
    /// and <see cref="ISetupItem.FullName">FullName</see> (that identify the item) can not be changed through this interface.
    /// </summary>
    public interface IMutableSetupBaseItem : ISetupItem
    { 
        /// <summary>
        /// Gets a mutable list of items that this item requires.
        /// </summary>
        new IDependentItemList Requires { get; }

        /// <summary>
        /// Gets a mutable list of items that are required by this item.
        /// </summary>
        new IDependentItemList RequiredBy { get; }

        /// <summary>
        /// Gets a mutable list of groups to which this item belongs.
        /// </summary>
        new IDependentItemGroupList Groups { get; }

        /// <summary>
        /// Gets or sets the container to which this item belongs.
        /// </summary>
        new IDependentItemContainerRef Container { get; set; }
       
    }
}
