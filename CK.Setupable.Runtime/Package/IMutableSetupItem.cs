using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// A mutable version of an <see cref="ISetupItem"/>.
    /// Note that the specialized <see cref="IMutableSetupItemGroup"/> offers children collection.
    /// Its <see cref="IContextLocNaming.Context">Context</see>, <see cref="IContextLocNaming.Location">Location</see>, <see cref="IContextLocNaming.Name">Name</see> 
    /// and <see cref="ISetupItem.FullName">FullName</see> (that identify the item) and <see cref="ItemKind"/> can not be changed through this interface.
    /// </summary>
    public interface IMutableSetupItem : ISetupItem
    {
        /// <summary>
        /// Gets whether this object must be considered as a <see cref="IDependentItem"/>, a <see cref="IDependentItemGroup"/> or a <see cref="IDependentItemContainer"/>
        /// whatever its actual type is.
        /// </summary>
        DependentItemKind ItemKind { get; }
        
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

        /// <summary>
        /// Gets or sets the generalization of this item.
        /// </summary>
        new IDependentItemRef Generalization { get; set; }
        
    }
}
