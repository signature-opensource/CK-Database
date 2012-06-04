using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Defines a reference to a container. Since a <see cref="IDependentItemContainer"/> is 
    /// its own IDependentItemContainerRef, when the object container is known it can be used
    /// as a (non optional) reference. The struct <see cref="DependentItemContainerRef"/> must be used for 
    /// optional or pure named reference.
    /// </summary>
    public interface IDependentItemContainerRef : IDependentItemRef
    {
        /// <summary>
        /// Gets a name that uniquely identifies the container. It must be not null.
        /// </summary>
        new string FullName { get; }

    }
}
