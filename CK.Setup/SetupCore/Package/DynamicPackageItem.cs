using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Fully mutable <see cref="IDependentItemContainer"/> and <see cref="IVersionnedItem"/> with a an optional associated <see cref="Model"/> package
    /// and  configurable type for the associated <see cref="SetupDriver"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DynamicContainerItem"/> can be used a pure mutable Container is needed (no versions nor associated model).
    /// </remarks>
    public class DynamicPackageItem : PackageItemBase, IDependentItemContainerTyped, IDependentItemDiscoverer
    {
        string _fullName;
        PackageModelItem _model;
        object _driverType;
        DependentItemType _dynamicType; 

        /// <summary>
        /// Initializes a new dynamic package with <see cref="ItemKind"/> set to <see cref="DependentItemType.Container"/>.
        /// </summary>
        /// <param name="itemType">The <see cref="IVersionedItem.ItemType"/> for this item.</param>
        /// <param name="driverType">
        /// Type of the driver to use. Can be the <see cref="Type"/> itself or the Assembly Qualified Name of the type.
        /// When null, the type of <see cref="SetupDriver"/> is asumed.
        /// </param>
        public DynamicPackageItem( string itemType, object driverType = null )
            : base( itemType )
        {
            _dynamicType = DependentItemType.Container;
            _driverType = driverType ?? typeof( SetupDriver );
        }

        /// <summary>
        /// Gets the optional <see cref="PackageModelItem"/> for this <see cref="DynamicPackageItem"/>.
        /// It is null (the default) if this package has no Model: use <see cref="EnsureModel"/> to
        /// create the Model if needed.
        /// </summary>
        public PackageModelItem Model
        {
            get { return _model; }
        }

        /// <summary>
        /// Creates the associated <see cref="Model"/> package if it does not exist yet.
        /// </summary>
        /// <returns></returns>
        public PackageModelItem EnsureModel()
        {
            return _model ?? (_model = new PackageModelItem( this ));
        }

        /// <summary>
        /// Removes the <see cref="Model"/> (sets it to null).
        /// </summary>
        public void SupressModel()
        {
            _model = null;
        }

        /// <summary>
        /// Gets or sets the full name of this package.
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
            get { return _dynamicType; }
            set { _dynamicType = value; }
        }

        protected override string GetFullName()
        {
            return _fullName;
        }

        protected override DependentItemType GetDynamicType()
        {
            return _dynamicType;
        }

        protected override object StartDependencySort()
        {
            return _driverType;
        }

        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetOtherItemsToRegister()
        {
            return _model != null ? new ReadOnlyListMono<IDependentItem>( _model ) : null;
        }

    }


}

