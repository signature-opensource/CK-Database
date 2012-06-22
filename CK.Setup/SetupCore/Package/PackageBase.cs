using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{

    public abstract class PackageBase : MultiVersionManager, IPackage, IDependentItemContainerRef
    {
        string _itemType;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemList _children;
        IDependentItemContainerRef _container;

        public PackageBase()
            : this( "Package" )
        {
        }

        public PackageBase( string itemType )
        {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            _itemType = itemType;
        }

        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
        }

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
        /// made to <see cref="GetFullName"/> (exposed as <see cref="IDependentItem.FullName"/>) or 
        /// any other properties that participates to dependency.
        /// </summary>
        /// <returns></returns>
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

        IEnumerable<IDependentItemRef> IDependentItemContainer.Children
        {
            get { return _children; }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return PreviousNames; }
        }
    }

}
