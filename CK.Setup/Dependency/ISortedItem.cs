using System;
using System.Collections.Generic;

namespace CK.Setup
{

    /// <summary>
    /// A sorted item can be directly associated to a <see cref="IDependentItem"/>, a <see cref="IDependentItemContainer"/> 
    /// or can be the head for a container.
    /// </summary>
    public interface ISortedItem
    {
        /// <summary>
        /// Gets the index of this item among the others.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the rank of this item.
        /// </summary>
        int Rank { get; }

        /// <summary>
        /// Gets the full name of the item.
        /// It is suffixed by ".head" if this is a head.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the container to which this item belongs.
        /// Use <see cref="HeadForContainer"/> to get its head.
        /// </summary>
        ISortedItem Container { get; }

        /// <summary>
        /// Whether this is the head of a container.
        /// Use <see cref="ContainerForHead"/> to get the associated container.
        /// </summary>
        bool IsContainerHead { get; }

        /// <summary>
        /// Whether this is a container.
        /// </summary>
        bool IsContainer { get; }

        /// <summary>
        /// Gets the head of a container if this item is a container (null otherwise).
        /// </summary>
        ISortedItem HeadForContainer { get; }

        /// <summary>
        /// Gets the container if this item is a head  (null otherwise).
        /// </summary>
        ISortedItem ContainerForHead { get; }

        /// <summary>
        /// Gets the associated item.
        /// </summary>
        IDependentItem Item { get; }

        /// <summary>
        /// Gets the requirements of the item. Combines direct <see cref="IDependentItem.Requires"/>
        /// and <see cref="IDependentItem.RequiredBy"/> declared by existing other items.
        /// </summary>
        /// <remarks>
        /// There can be duplicates in this list if the item A requires B and B claims also to be
        /// required by A.
        /// </remarks>
        IEnumerable<string> Requires { get; }
    }
}
