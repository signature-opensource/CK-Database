using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Mutable package implementation: any property can be changed (version information is handled
    /// by the base <see cref="MultiVersionManager"/>) except FullName and <see cref="IDependentItemContainerAsk.ThisIsNotAContainer"/> (that can be 
    /// used to dynamically refuse to be referenced as a Container by other items) that must be provided through implementations of abstract methods.
    /// </summary>
    /// <remarks>
    /// The <see cref="ContainerItemBase"/> must be used for container that do not have versions.
    /// </remarks>
    public abstract class PackageItemBase : MultiVersionManager, IMutableDependentItem, IDependentItemContainerAsk, IPackageItem, IDependentItemContainerRef
    {
        string _itemType;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemList _children;
        IDependentItemContainerRef _container;
        IDependentItemRef _generalization;

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
        /// Gets or sets the generalization of this package.
        /// </summary>
        public IDependentItemRef Generalization
        {
            get { return _generalization; }
            set { _generalization = value; }
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
        /// Gets whether this container is actually NOT a container.
        /// When true, if an item declares this item as its container, an error is 
        /// raised during the ordering of the dependency graph.
        /// </summary>
        protected abstract bool GetThisIsNotAContainer();

        /// <summary>
        /// Called at the very beginning of the setup phasis, before <see cref="IDependentItem.FullName"/> is used to planify the setup. 
        /// This start method has been already called on direct dependencies <see cref="Container"/>, <see cref="Generalization"/>
        /// and <see cref="Requires"/> if they are <see cref="IDependentItem"/> (and not strings).
        /// </summary>
        /// <returns>
        /// Must return the <see cref="Type"/> of the setup driver (specialization of <see cref="SetupDriver"/>), or its assembly qualified name.
        /// By default, returns the type of <see cref="SetupDriver"/>.
        /// </returns>
        protected virtual object StartDependencySort()
        {
            return typeof( SetupDriver );
        }

        object IDependentItem.StartDependencySort()
        {
            return StartDependencySort();
        }

        bool IDependentItemContainerAsk.ThisIsNotAContainer
        {
            get { return GetThisIsNotAContainer(); }
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
