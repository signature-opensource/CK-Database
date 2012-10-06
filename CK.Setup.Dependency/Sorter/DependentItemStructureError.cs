using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{

    /// <summary>
    /// Describes <see cref="IDependentItem.Container"/> related errors.
    /// </summary>
    [Flags]
    public enum DependentItemStructureError
    {
        /// <summary>
        /// No error occured.
        /// </summary>
        None,
        /// <summary>
        /// A <see cref="IDependentItem.Container"/> is a <see cref="IDependentItemContainerRef"/> and 
        /// its <see cref="IDependentItemContainerRef.FullName">FullName</see> does not exist.
        /// </summary>
        MissingNamedContainer = 1,
        /// <summary>
        /// A <see cref="IDependentItem.Container"/> is a <see cref="IDependentItemContainerRef"/> 
        /// that reference a simple <see cref="IDependentItem"/> and not a container.
        /// </summary>
        ExistingItemIsNotAContainer = 2,
        /// <summary>
        /// A <see cref="IDependentItem.Container"/> references a <see cref="IDependentItemContainerTyped"/> that 
        /// declares to not be a container.
        /// </summary>
        ExistingContainerAskedToNotBeAContainer = 4,
        /// <summary>
        /// The item has more than one container: more than one container declare it in their <see cref="IDependentItemContainer.Children"/>
        /// or the <see cref="IDependentItem.Container"/> declared by the item itself (if not null) is another container or another container appears
        /// in the <see cref="IDependentItem.Groups"/> list.
        /// </summary>
        MultipleContainer = 8,
        /// <summary>
        /// A <see cref="IDependentItemContainer.Children"/> contains a <see cref="IDependentItemContainerRef"/> and 
        /// its <see cref="IDependentItemContainerRef.FullName">FullName</see> has not been registered.
        /// </summary>
        MissingNamedChild = 16,
        /// <summary>
        /// A dependency can not be found.
        /// </summary>
        MissingDependency = 32,
        /// <summary>
        /// A generalization can not be found.
        /// </summary>
        MissingGeneralization = 64,
        /// <summary>
        /// Two items or more use the same <see cref="IDependentItem.FullName"/>.
        /// </summary>
        Homonym = 128,
        /// <summary>
        /// A <see cref="IDependentItemContainerTyped"/> that declares to not be a group (<see cref="IDependentItemContainerTyped.ItemKind"/> is <see cref="DependentItemType.SimpleItem"/>)
        /// has items in its <see cref="IDependentItemContainer.Children"/> collection.
        /// </summary>
        ContainerAskedToNotBeAGroupButContainsChildren = 256,
        /// <summary>
        /// A group in <see cref="IDependentItem.Groups"/> declares to not be a group (<see cref="IDependentItemContainerTyped.ItemKind"/> is <see cref="DependentItemType.SimpleItem"/>).
        /// </summary>
        DeclaredGroupRefusedToBeAGroup = 512,
        /// <summary>
        /// A required named group is not registered.
        /// </summary>
        MissingNamedGroup = 1024,
    }
}
