#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\PackageItemBase.cs) is part of CK-Database. 
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
    /// Mutable package implementation: any property can be changed (version information is handled
    /// by the base <see cref="MultiVersionManager"/>) except <see cref="IDependentItemContainerTyped.ItemKind"/> (that can be 
    /// used to dynamically refuse to be referenced as a Container by other items) that must be provided through implementations of abstract methods.
    /// </summary>
    /// <remarks>
    /// The <see cref="DynamicContainerItem"/> must be used for container that do not have versions.
    /// </remarks>
    public abstract class PackageItemBase : DynamicDependentItem, IMutableSetupItemContainer, IPackageItem, IDependentItemContainerRef
    {
        DependentItemList _children;

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
            : base( itemType )
        {
        }
        
        /// <summary>
        /// Gets a mutable list of children for this package.
        /// </summary>
        public IDependentItemList Children
        {
            get { return _children ?? (_children = new DependentItemList()); }
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        object IDependentItem.StartDependencySort()
        {
            return StartDependencySort();
        }

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, Context, Location ) ); }
        }

    }

}
