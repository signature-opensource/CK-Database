#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\DynamicPackageItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Fully mutable <see cref="IDependentItemContainer"/> and <see cref="IVersionedItem"/> with optional associated <see cref="Model"/> 
    /// and <see cref="ObjectsPackage"/> packages (that are <see cref="AutoDependentPackageItem"/>) and  configurable type for the associated <see cref="GenericItemSetupDriver"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DynamicContainerItem"/> can be used if a pure mutable Container is needed (no versions nor associated AutoDependentPackageItem).
    /// </remarks>
    public class DynamicPackageItem : PackageItemBase, IDependentItemContainerTyped, IDependentItemDiscoverer<ISetupItem>
    {
        AutoDependentPackageItem _model;
        AutoDependentPackageItem _objects;
        object _driverType;

        /// <summary>
        /// Initializes a new dynamic package with <see cref="ItemKind"/> set to <see cref="DependentItemKind.Container"/>.
        /// </summary>
        /// <param name="itemType">The <see cref="IVersionedItem.ItemType"/> for this item.</param>
        /// <param name="driverType">
        /// Type of the driver to use. Can be the <see cref="Type"/> itself or the Assembly Qualified Name of the type.
        /// When null, the type of <see cref="GenericItemSetupDriver"/> is asumed.
        /// </param>
        public DynamicPackageItem( string itemType, object driverType = null )
            : base( itemType )
        {
            _driverType = driverType ?? typeof( GenericItemSetupDriver );
            ItemKind = DependentItemKind.Container;
        }

        /// <summary>
        /// Gets the optional <see cref="AutoDependentPackageItem"/> "Model" for this <see cref="DynamicPackageItem"/>.
        /// It is null (the default) if this package has no Model: use <see cref="EnsureModel"/> to
        /// create the Model if needed.
        /// </summary>
        public AutoDependentPackageItem Model
        {
            get { return _model; }
        }

        /// <summary>
        /// Creates the associated <see cref="Model"/> package if it does not exist yet.
        /// </summary>
        /// <returns></returns>
        public AutoDependentPackageItem EnsureModel()
        {
            return _model ?? (_model = new AutoDependentPackageItem( this, true, "Model", "Model." ));
        }

        /// <summary>
        /// Removes the <see cref="Model"/> (sets it to null).
        /// </summary>
        public void SupressModel()
        {
            _model = null;
        }

        /// <summary>
        /// Gets the optional <see cref="AutoDependentPackageItem"/> "Objects" for this <see cref="DynamicPackageItem"/>.
        /// It is null (the default) if this package has no associated "Objects" package: use <see cref="EnsureModel"/> to
        /// create the Model if needed.
        /// </summary>
        public AutoDependentPackageItem ObjectsPackage
        {
            get { return _objects; }
        }

        /// <summary>
        /// Creates the associated <see cref="ObjectsPackage"/> package if it does not exist yet.
        /// </summary>
        /// <returns></returns>
        public AutoDependentPackageItem EnsureObjectsPackage()
        {
            return _objects ?? (_objects = new AutoDependentPackageItem( this, false, "Objects", "Objects." ));
        }

        /// <summary>
        /// Removes the <see cref="ObjectsPackage"/> (sets it to null).
        /// </summary>
        public void SupressObjectsPackage()
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

        protected override object StartDependencySort()
        {
            return _driverType;
        }

        IEnumerable<ISetupItem> IDependentItemDiscoverer<ISetupItem>.GetOtherItemsToRegister()
        {
            if( _objects == null )
            {
                return _model != null ? new CKReadOnlyListMono<ISetupItem>( _model ) : null;
            }
            else if( _model == null )
            {
                return new CKReadOnlyListMono<ISetupItem>( _objects );
            }
            else 
            {
                return new []{ _model, _objects };
            }
        }

    }


}

