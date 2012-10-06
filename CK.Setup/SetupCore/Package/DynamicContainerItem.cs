using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Standard implementation of a mutable <see cref="IDependentItemContainer"/> that is NOT a <see cref="IVersionedItem"/>.
    /// This class implements the minimal behavior for an item container: FullName, Requires, RequiredBy, Container, Generalization and Children, all 
    /// of them being writable, plus the <see cref="IDependentItemContainerTyped.ItemKind"/> that can be used to dynamically refuse to be referenced
    /// as a Container by other items.
    /// </summary>
    /// <remarks>
    /// The <see cref="DynamicPackageItem"/> must be used for container that have multiple versions and an optional associated model.
    /// </remarks>
    public class DynamicContainerItem : ContainerItemBase
    {
        string _fullName;
        DependentItemType _itemKind; 

        public DynamicContainerItem()
        {
            _itemKind = DependentItemType.Container;
        }

        /// <summary>
        /// Gets or sets the full name of this container.
        /// It must be not null.
        /// </summary>
        public string FullName
        {
            get { return _fullName; }
            set { _fullName = value ?? String.Empty; }
        }

        /// <summary>
        /// Gets or sets whether this container is actually NOT a Container or even not a Group.
        /// When not <see cref="DependentItemType.Container"/>, if an item declares this item as its container, an error is raised 
        /// during the ordering of the dependency graph.
        /// </summary>
        public DependentItemType ItemKind
        {
            get { return _itemKind; }
            set { _itemKind = value; }
        }

        protected override string GetFullName()
        {
            return _fullName;
        }

        protected override DependentItemType GetItemKind()
        {
            return _itemKind;
        }
    }


}

