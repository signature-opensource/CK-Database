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
    public interface IDependentItem
    {
        /// <summary>
        /// Gets a name that uniquely identifies the item. 
        /// It must be not null.
        /// </summary>
        string FullName { get; }

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
        /// Gets this item's dependencies. Can be null if no such dependency exists.
        /// </summary>
        /// <remarks>
        /// When <see cref="IDependentItemRef.Optional"/> is true, if it is not found, this
        /// will not be an error (see <see cref="DependentItemIssue.MissingDependencies"/>).
        /// </remarks>
        IEnumerable<IDependentItemRef> Requires { get; }

        /// <summary>
        /// Gets the revert dependencies (an item can specify that it is itself required by another one). 
        /// A "RequiredBy" constraint is optional: a missing "RequiredBy" is not an error (it is considered 
        /// as a reverted optional dependency).
        /// Can be null if no such dependency exists.
        /// </summary>
        IEnumerable<IDependentItemRef> RequiredBy { get; }

        /// <summary>
        /// Allows the dependent item to prepare itself before ordering. The returned object (if any)
        /// is made available after the sort in <see cref="ISortedItem.StartValue"/>.
        /// </summary>
        /// <returns>Any object that has to be associated to this item and a <see cref="DependencySorter.OrderItems"/> call.</returns>
        object StartDependencySort();
    }
}

