using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Collection part of the composite <see cref="IDependentItem"/>. 
    /// It only has to expose its <see cref="Children"/>.
    /// </summary>
    public interface IDependentItemContainer : IDependentItem, IDependentItemContainerRef
    {
        /// <summary>
        /// Gets a name that uniquely identifies the container. It must be not null.
        /// This is a redifinition to remove the ambiguity between <see cref="IDependentItem.FullName"/>
        /// and <see cref="IDependentItemContainerRef.FullName"/> that are actually the same.
        /// </summary>
        new string FullName { get; }

        /// <summary>
        /// Gets a list of children. Can be null or empty (see remarks).
        /// </summary>
        /// <remarks>
        /// The <see cref="DependencySorter"/> uses this list to discover the original <see cref="IDependentItem"/> to order.
        /// Provided that each and every item is submitted to the <see cref="DependencySorter.OrderItems"/>, this collection 
        /// can be null or empty (even if some <see cref="IDependentItem.Container"/> refer to this container).
        /// </remarks>
        IEnumerable<IDependentItemRef> Children { get; }

    }
}
