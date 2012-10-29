using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Mutable item implementation: any property can be changed (version information is handled
    /// by the base <see cref="MultiVersionManager"/>) except FullName that must be provided through 
    /// implementations of abstract methods.
    /// </summary>
    public abstract class DynamicDependentItem : MultiVersionManager, IMutableDependentItem, IVersionedItem, IDependentItemRef
    {
        string _itemType;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemGroupList _groups;
        IDependentItemContainerRef _container;
        IDependentItemRef _generalization;

        /// <summary>
        /// Initializes a new mutable item with a given <see cref="IVersionedItem.ItemType"/>.
        /// </summary>
        /// <param name="itemType">Type of the item.</param>
        protected DynamicDependentItem( string itemType )
        {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            _itemType = itemType;
        }

        /// <summary>
        /// Gets a mutable list of items that this item requires.
        /// </summary>
        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of items that are required by this one.
        /// </summary>
        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of groups to which this item belongs.
        /// </summary>
        public IDependentItemGroupList Groups
        {
            get { return _groups ?? (_groups = new DependentItemGroupList()); }
        }

        /// <summary>
        /// Gets or sets the container to which this item belongs.
        /// </summary>
        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
        }

        /// <summary>
        /// Gets or sets the generalization of this item.
        /// </summary>
        public IDependentItemRef Generalization
        {
            get { return _generalization; }
            set { _generalization = value; }
        }
        
        /// <summary>
        /// Must return the full name of this item.
        /// It can be computed by <see cref="StartDependencySort"/>.
        /// </summary>
        /// <returns>This full name.</returns>
        protected abstract string GetFullName();

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

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups
        {
            get { return _groups; }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return PreviousNames; }
        }
    }

}
