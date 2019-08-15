using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Fully mutable <see cref="IDependentItemContainer"/> and <see cref="IVersionedItem"/> with optional associated <see cref="ModelPackage"/> 
    /// and <see cref="ObjectsPackage"/> packages (that are <see cref="AutoDependentPackageItem"/>) and  configurable type for the associated <see cref="SetupItemDriver"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DynamicContainerItem"/> can be used if a pure mutable Container is needed (no versions nor associated AutoDependentPackageItem).
    /// </remarks>
    public class DynamicPackageItem : MultiVersionDependentItem, IMutableSetupItemContainer, IPackageItem, IDependentItemContainerRef, IDependentItemDiscoverer<ISetupItem>
    {
        IDependentItemList _children;
        AutoDependentPackageItem _model;
        AutoDependentPackageItem _objects;
        object _driverType;

        /// <summary>
        /// Initializes a new dynamic package with <see cref="ItemKind"/> set to <see cref="DependentItemKind.Container"/>.
        /// </summary>
        /// <param name="itemType">The <see cref="IVersionedItem.ItemType"/> for this item.</param>
        /// <param name="driverType">
        /// Type of the driver to use. Can be the <see cref="Type"/> itself or the Assembly Qualified Name of the type.
        /// When null, the type of <see cref="SetupItemDriver"/> is asumed.
        /// </param>
        public DynamicPackageItem( string itemType, object driverType = null )
            : base( itemType )
        {
            _driverType = driverType ?? typeof( SetupItemDriver );
            ItemKind = DependentItemKind.Container;
        }

        /// <summary>
        /// Gets a mutable list of children for this package.
        /// </summary>
        public IDependentItemList Children => _children ?? (_children = DependentItemListFactory.CreateItemList()); 

        bool IDependentItemRef.Optional => false;

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, Context, Location ) ); }
        }

        /// <summary>
        /// Gets the optional <see cref="AutoDependentPackageItem"/> "Model" for this <see cref="DynamicPackageItem"/>.
        /// It is null (the default). If this package must have a Model package, use <see cref="EnsureModelPackage"/> to
        /// create it.
        /// </summary>
        public AutoDependentPackageItem ModelPackage => _model;

        /// <summary>
        /// Creates the associated <see cref="ModelPackage"/> package if it does not exist yet.
        /// </summary>
        /// <returns>The <see cref="AutoDependentPackageItem"/>.</returns>
        public virtual AutoDependentPackageItem EnsureModelPackage()
        {
            return _model ?? (_model = new AutoDependentPackageItem( this, true, "Model", "Model." ));
        }

        /// <summary>
        /// Removes the <see cref="ModelPackage"/> (sets it to null).
        /// </summary>
        public virtual void SupressModelPackage()
        {
            _model = null;
        }

        /// <summary>
        /// Gets the optional <see cref="AutoDependentPackageItem"/> "Objects" for this <see cref="DynamicPackageItem"/>.
        /// It is null (the default). If this package must have an "Objects" package, use <see cref="EnsureObjectsPackage"/> to
        /// create it.
        /// </summary>
        public AutoDependentPackageItem ObjectsPackage => _objects; 

        /// <summary>
        /// Creates the associated <see cref="ObjectsPackage"/> package if it does not exist yet.
        /// </summary>
        /// <returns>The <see cref="AutoDependentPackageItem"/>.</returns>
        public virtual AutoDependentPackageItem EnsureObjectsPackage()
        {
            return _objects ?? (_objects = new AutoDependentPackageItem( this, false, "Objects", "Objects." ));
        }

        /// <summary>
        /// Removes the <see cref="ObjectsPackage"/> (sets it to null).
        /// </summary>
        public virtual void SupressObjectsPackage()
        {
            _objects = null;
        }

        /// <summary>
        /// Gets or sets the kind of this item.
        /// </summary>
        public new DependentItemKind ItemKind
        {
            get { return base.ItemKind; }
            set { base.ItemKind = value; }
        }

        /// <summary>
        /// Called by the <see cref="DependencySorter"/>.
        /// The object return that is associated to this item is its driver type.
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <returns>The driver type.</returns>
        protected override object StartDependencySort( IActivityMonitor m )
        {
            return _driverType;
        }

        IEnumerable<ISetupItem> IDependentItemDiscoverer<ISetupItem>.GetOtherItemsToRegister()
        {
            if( _objects == null )
            {
                return _model != null ? new [] { _model } : null;
            }
            else if( _model == null )
            {
                return new [] { _objects };
            }
            else 
            {
                return new []{ _model, _objects };
            }
        }

    }


}

