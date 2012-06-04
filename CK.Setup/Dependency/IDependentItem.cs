using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// An item that is identified by a <see cref="IDependentItemRef.FullName">FullName</see>, can be in 
    /// a <see cref="IDependentItemContainer"/> and have dependencies 
    /// onto other items. The <see cref="DependencySorter"/> is used to 
    /// order such items based on their dependencies.
    /// </summary>
    public interface IDependentItem : IDependentItemRef
    {
        /// <summary>
        /// Gets a reference to the container to which this item belongs. 
        /// Null if this item does not belong to a container.
        /// </summary>
        /// <remarks>
        /// If a container exists, this item may belong to <see cref="IDependentItemContainer.Children"/>
        /// (but it is not mandatory as long as the <see cref="DependencySorter"/> is concerned).
        /// </remarks>
        IDependentItemContainerRef Container { get; }

        /// <summary>
        /// Gets names of dependencies. Can be null if no such dependency exists.
        /// When the name starts with '?', it is an optional dependency: if it is not found, this
        /// will not be an error (see <see cref="DependentItemIssue.MissingDependencies"/>).
        /// </summary>
        IEnumerable<string> Requires { get; }

        /// <summary>
        /// Gets names of revert dependencies (an item can specify that it is itself required by another one). 
        /// A "RequiredBy" constraint is optional: a missing "RequiredBy" is not an error (it is considered 
        /// as a reverted optional dependency).
        /// Can be null if no such dependency exists.
        /// </summary>
        IEnumerable<string> RequiredBy { get; }
    }
}

