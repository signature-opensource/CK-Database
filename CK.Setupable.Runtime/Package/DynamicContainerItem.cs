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
    /// The <see cref="DynamicPackageItem"/> must be used for container that have multiple versions and optional associated "Model" and "Objects".
    /// </remarks>
    public abstract class DynamicContainerItem : IMutableSetupItemContainer, ISetupItem, IDependentItemContainerRef
    {
        ContextLocNameStructImpl _name;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemList _children;
        DependentItemGroupList _groups;
        IDependentItemContainerRef _container;
        IDependentItemRef _generalization;
        DependentItemKind _itemKind;
        object _driverType;

        /// <summary>
        /// Initializes a new dynamic package with <see cref="ItemKind"/> set to <see cref="DependentItemKind.Container"/>.
        /// </summary>
        /// <param name="driverType">
        /// Type of the driver to use. Can be the <see cref="Type"/> itself or the Assembly Qualified Name of the type.
        /// When null, the type of <see cref="SetupItemDriver"/> is asumed.
        /// </param>
        public DynamicContainerItem( object driverType = null )
        {
            _driverType = driverType ?? typeof( SetupItemDriver );
            ItemKind = DependentItemKind.Container;
        }

        /// <summary>
        /// Gets or sets the context identifier of this container. 
        /// Can be null (unknown context) or empty (the default context).
        /// When set, <see cref="FullName"/> is automatically updated.
        /// </summary>
        public string Context
        {
            get { return _name.Context; }
            set { _name.Context = value; }
        }

        /// <summary>
        /// Gets or sets the location of this container. 
        /// Can be null (unknown location) or empty (the root location).
        /// When set, <see cref="FullName"/> is automatically updated.
        /// </summary>
        public string Location
        {
            get { return _name.Location; }
            set { _name.Location = value; }
        }

        /// <summary>
        /// Gets or sets the name of this container. <see cref="FullName"/> is automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string Name
        {
            get { return _name.Name; }
            set { DefaultContextLocNaming.ThrowIfTransformArg( value ); _name.Name = value; }
        }

        /// <summary>
        /// Gets or sets the full name of this container. <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/> are automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string FullName
        {
            get { return _name.FullName; }
            set { DefaultContextLocNaming.ThrowIfTransformArg( value ); _name.FullName = value; }
        }

        string IContextLocNaming.TransformArg => null;

        /// <summary>
        /// Gets or sets whether this container is actually NOT a Container or even not a Group.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="DependentItemKind.Container"/>. 
        /// When this ItemKind is not <see cref="DependentItemKind.Container"/> and an item declares this item as its container, or 
        /// when it is <see cref="DependentItemKind.Item"/> and this item is referenced as a group, an error is raised 
        /// during the ordering of the dependency graph.
        /// </remarks>
        public DependentItemKind ItemKind
        {
            get { return _itemKind; }
            set { _itemKind = value; }
        }

        /// <summary>
        /// Gets or sets the container to which this container belongs.
        /// </summary>
        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
        }

        /// <summary>
        /// Gets or sets the generalization of this container.
        /// </summary>
        public IDependentItemRef Generalization
        {
            get { return _generalization; }
            set { _generalization = value; }
        }

        /// <summary>
        /// Gets a mutable list of items that this container requires.
        /// </summary>
        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of items that are required by this container.
        /// </summary>
        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of groups to which this package belongs.
        /// </summary>
        public IDependentItemGroupList Groups => _groups ?? (_groups = new DependentItemGroupList()); 

        /// <summary>
        /// Gets a mutable list of children for this package.
        /// </summary>
        public IDependentItemList Children => _children ?? (_children = new DependentItemList()); 

        /// <summary>
        /// Called at the very beginning of the setup phasis, before <see cref="IDependentItem.FullName"/> is used to planify the setup. 
        /// This start method has been already called on direct dependencies <see cref="Container"/>, <see cref="Generalization"/>
        /// and <see cref="Requires"/> if they are <see cref="IDependentItem"/> (and not strings).
        /// </summary>
        /// <returns>
        /// Must return the <see cref="Type"/> of the setup driver (specialization of <see cref="SetupItemDriver"/>), or its assembly qualified name.
        /// By default, returns the type of <see cref="SetupItemDriver"/>.
        /// </returns>
        protected virtual object StartDependencySort() => _driverType;

        object IDependentItem.StartDependencySort() => StartDependencySort();

        bool IDependentItemRef.Optional => false; 

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
        
        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _name.Context, _name.Location ) ); }
        }
    }


}

