using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// A mutable version of an <see cref="IDependentItemContainerTyped"/>.
    /// The <see cref="IDependentItem.FullName"/> (that identifies the item) and the <see cref="IDependentItemContainerTyped.ItemKind">ItemKind</see> can not be changed through this interface.
    /// </summary>
    public interface IMutableDependentItemContainerTyped : IMutableDependentItem, IDependentItemContainerTyped, IDependentItemContainerRef
    {
        /// <summary>
        /// Gets a mutable list of items that this item requires.
        /// </summary>
        new IDependentItemList Children { get; }
    }
}
