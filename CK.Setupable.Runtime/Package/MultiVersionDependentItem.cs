#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\DynamicDependentItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
    public abstract class MultiVersionDependentItem : MultiVersionManager, IMutableSetupItem, IVersionedItem, IDependentItemRef
    {
        ContextLocNameStructImpl _name;
        string _itemType;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemGroupList _groups;
        IDependentItemContainerRef _container;
        IDependentItemRef _generalization;
        DependentItemKind _itemKind;

        /// <summary>
        /// Initializes a new mutable item with a given <see cref="IVersionedItem.ItemType"/>.
        /// </summary>
        /// <param name="itemType">Type of the item.</param>
        protected MultiVersionDependentItem( string itemType )
        {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            _itemType = itemType;
            _name = new ContextLocNameStructImpl( String.Empty );
            _itemKind = DependentItemKind.Item;
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
            set { DefaultContextLocNaming.ThrowIfTransformArg( value ); _name.Name = value; }
        }

        /// <summary>
        /// Gets or sets the full name of this package. <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/> are automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string FullName
        {
            get { return _name.FullName; }
            set { DefaultContextLocNaming.ThrowIfTransformArg( value ); _name.FullName = value; }
        }

        string IContextLocNaming.TransformArg => null;


        /// <summary>
        /// Gets whether this item is a simple Item, a Group or a Container.
        /// </summary>
        /// <remarks>
        /// This is <see cref="DependentItemKind.Item"/> at this level but can be changed by specialized classes. 
        /// When this ItemKind is not <see cref="DependentItemKind.Container"/> and an item declares this item as its container, or 
        /// when it is <see cref="DependentItemKind.Item"/> and this item is referenced as a group, an error is raised 
        /// during the ordering of the dependency graph.
        /// </remarks>
        public DependentItemKind ItemKind
        {
            get { return _itemKind; }
            protected set { _itemKind = value; }
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
        /// Called at the very beginning of the setup phase, before <see cref="IDependentItem.FullName"/> is used to planify the setup. 
        /// This start method has been already called on direct dependencies <see cref="Container"/>, <see cref="Generalization"/>
        /// and <see cref="Requires"/> if they are <see cref="IDependentItem"/> (and not strings).
        /// </summary>
        /// <returns>
        /// Must return the <see cref="Type"/> of the setup driver (specialization of <see cref="SetupItemDriver"/>), or its assembly qualified name.
        /// By default, returns the type of <see cref="SetupItemDriver"/>.
        /// </returns>
        protected virtual object StartDependencySort( IActivityMonitor m ) => typeof( SetupItemDriver );

        object IDependentItem.StartDependencySort( IActivityMonitor m ) => StartDependencySort( m );

        string IVersionedItem.ItemType => _itemType; 

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

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return PreviousNames.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _name.Context, _name.Location ) ); }
        }

    }

}
