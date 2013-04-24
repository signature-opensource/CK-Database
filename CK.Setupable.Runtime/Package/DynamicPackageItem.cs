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
        PackageModelItem _model;
        object _driverType;

        /// <summary>
        /// Initializes a new dynamic package with <see cref="ItemKind"/> set to <see cref="DependentItemKind.Container"/>.
        /// </summary>
        /// <param name="itemType">The <see cref="IVersionedItem.ItemType"/> for this item.</param>
        /// <param name="driverType">
        /// Type of the driver to use. Can be the <see cref="Type"/> itself or the Assembly Qualified Name of the type.
        /// When null, the type of <see cref="SetupDriver"/> is asumed.
        /// </param>
        public DynamicPackageItem( string itemType, object driverType = null )
            : base( itemType )
        {
            _driverType = driverType ?? typeof( SetupDriver );
            ItemKind = DependentItemKind.Container;
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
        /// Gets or sets the kind of this item.
        /// </summary>
        public new DependentItemKind ItemKind
        {
            get { return base.ItemKind; }
            set { base.ItemKind = value; }
        }

        protected override object StartDependencySort()
        {
            return _driverType;
        }

        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetOtherItemsToRegister()
        {
            return _model != null ? new CKReadOnlyListMono<IDependentItem>( _model ) : null;
        }

    }


}

