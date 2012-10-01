using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// A mutable version of an <see cref="IDependentItem"/>
    /// </summary>
    public interface IMutableDependentItem : IDependentItem
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
        /// Gets or sets the container to which this item belongs.
        /// </summary>
        new IDependentItemContainerRef Container { get; set; }

        /// <summary>
        /// Gets or sets the generalization of this item.
        /// </summary>
        new IDependentItemRef Generalization { get; set; }
        
    }
}
