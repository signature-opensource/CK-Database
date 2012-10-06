using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Defines a reference to a container. 
    /// </summary>
    /// <remarks>
    /// A <see cref="IDependentItemContainer"/> implementation should implement this 
    /// (it is then its own IDependentItemContainerRef): when the container object exists it can be used
    /// as a (non optional) reference. The struct <see cref="NamedDependentItemContainerRef"/> must be used for 
    /// optional or pure named reference.
    /// </remarks>
    public interface IDependentItemContainerRef : IDependentItemGroupRef
    {
    }
}
