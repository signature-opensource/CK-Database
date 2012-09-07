using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Mutable package implementation: any property can be changed (version information is handled
    /// by the base <see cref="MultiVersionManager"/>).
    /// </summary>
    public abstract class PackageItemBase : MultiVersionManager, IPackageItem, IDependentItemContainerRef
    {
        string _itemType;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemList _children;
        IDependentItemContainerRef _container;

        /// <summary>
        /// Initializes a new mutable package with 'Package' as its <see cref="IVersionedItem.ItemType"/>.
        /// </summary>
        protected PackageItemBase()
            : this( "Package" )
        {
        }

        /// <summary>
        /// Initializes a new mutable package with a given <see cref="IVersionedItem.ItemType"/>.
        /// </summary>
        /// <param name="itemType">Type of the package.</param>
        protected PackageItemBase( string itemType )
        {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            _itemType = itemType;
        }

        /// <summary>
        /// Gets a mutable list of items that this package requires.
        /// </summary>
        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of items that are required by this package.
        /// </summary>
        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        /// <summary>
        /// Gets or sets the container to which this package belongs.
        /// </summary>
        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
        }

        /// <summary>
        /// Gets a mutable list of children for this package.
        /// </summary>
        public IDependentItemList Children
        {
            get { return _children ?? (_children = new DependentItemList()); }
        }

        /// <summary>
        /// Must return the full name of this item.
        /// It can be computed by <see cref="StartDependencySort"/>.
        /// </summary>
        /// <returns>This full name.</returns>
        protected abstract string GetFullName();

        /// <summary>
        /// Called at the very beginning of the setup phasis, before any calls are 
        /// made to <see cref="GetFullName"/> (exposed as <see cref="IDependentItem.FullName"/>). 
        /// This start has been already called on direct dependencies <see cref="Container"/> 
        /// and <see cref="Requires"/> if they are <see cref="IDependentItem"/> (and not strings).
        /// </summary>
        /// <returns>Must return the <see cref="Type"/> of the setup driver (specialization of <see cref="DriverBase"/>), its fully qualified name.</returns>
        protected abstract object StartDependencySort();

        object IDependentItem.StartDependencySort()
        {
            return StartDependencySort();
        }

        string IVersionedItem.ItemType
        {
            get { return _itemType; }
        }
        
        string IDependentItem.FullName
        {
            get { return GetFullName(); }
        }

        string IDependentItemRef.FullName
        {
            get { return GetFullName(); }
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get { return _requires; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get { return _requiredBy; }
        }

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children; }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return PreviousNames; }
        }
    }

}
