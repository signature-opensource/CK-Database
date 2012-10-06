using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Used by <see cref="IDependentItemContainerTyped"/> to dynamically restrict its type.
    /// </summary>
    public enum DependentItemType
    {
        /// <summary>
        /// Unknown type can be used to dynamically adjust the behavior of the item.
        /// </summary>
        Unknown,

        /// <summary>
        /// Considers the item as a pure <see cref="IDependentItem"/>.
        /// </summary>
        SimpleItem,

        /// <summary>
        /// Considers the item as a <see cref="IDependentItemGroup"/>.
        /// </summary>
        Group,

        /// <summary>
        /// Considers the item as a <see cref="IDependentItemContainer"/>.
        /// </summary>
        Container
    }

}
