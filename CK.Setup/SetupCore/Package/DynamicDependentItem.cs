using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Mutable item implementation: any property can be changed (version information is handled
    /// by the base <see cref="MultiVersionManager"/>).
    /// </summary>
    public abstract class DynamicDependentItem : MultiVersionManager, IMutableSetupItem, IVersionedItem, IDependentItemRef
    {
        ContextLocNameStructImpl _name;
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
            _name = new ContextLocNameStructImpl();
        }

        /// <summary>
        /// Gets or sets the context identifier of this package. 
        /// Can be null (unknown context) or empty (the default context).
        /// When set, <see cref="FullName"/> is automatically updated.
        /// </summary>
        public string Context
        {
            get { return _name.Context; }
            set { _name.Context = value; }
        }

        /// <summary>
        /// Gets or sets the location of this package. 
        /// Can be null (unknown location) or empty (the root location).
        /// When set, <see cref="FullName"/> is automatically updated.
        /// </summary>
        public string Location
        {
            get { return _name.Location; }
            set { _name.Location = value; }
        }

        /// <summary>
        /// Gets or sets the name of this package. <see cref="FullName"/> is automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string Name
        {
            get { return _name.Name; }
            set { _name.Name = value; }
        }

        /// <summary>
        /// Gets or sets the full name of this package. <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/> are automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string FullName
        {
            get { return _name.FullName; }
            set { _name.FullName = value; }
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

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return _container.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _name.Context, _name.Location ) ); }
        }

        IDependentItemRef IDependentItem.Generalization
        {
            get { return _generalization.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _name.Context, _name.Location ) ); }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get { return _requires.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _name.Context, _name.Location ) ); }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get { return _requiredBy.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _name.Context, _name.Location ) ); }
        }

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups
        {
            get { return _groups.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _name.Context, _name.Location ) ); }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return PreviousNames.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _name.Context, _name.Location ) ); }
        }

    }

}
