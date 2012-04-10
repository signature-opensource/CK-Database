using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Defines a reference to an item. Since a <see cref="IDependentItem"/> is 
    /// its own IDependentItemRef, when the object item is known it can be used 
    /// as the reference. The struct <see cref="DependentItemRef"/> must be used for 
    /// pure named reference.
    /// </summary>
    public interface IDependentItemRef
    {
        /// <summary>
        /// Gets a name that uniquely identifies the item. It must be not null.
        /// </summary>
        string FullName { get; }

    }
}
