#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\DriverBase.cs) is part of CK-Database. 
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
    /// Abstract base class for setup drivers. 
    /// It exposes the <see cref="Item"/> that must be setup, its current <see cref="ExternalVersion"/> if any.
    /// </summary>
    /// <remarks>
    /// This class is not intended to be specialized outside this CK.Setupable.Runtime assembly: it is used as a base 
    /// class for the actual item driver (<see cref="GenericItemSetupDriver"/>) and by an internal class of the CK.Setupable.Engine assembly
    /// for handling heads of groups or containers.
    /// </remarks>
    public abstract class DriverBase
    {
        readonly ISortedItem<ISetupItem> _sortedItem;

        internal DriverBase( ISetupEngine engine, ISortedItem<ISetupItem> sortedItem, VersionedName externalVersion )
        {
            Engine = engine;
            _sortedItem = sortedItem;
            ExternalVersion = externalVersion;
            FullName = _sortedItem.FullName;
        }

        /// <summary>
        /// Gets the item to setup.
        /// This property is often redefined (masked with the new keyword in C#) to expose a more precise associated type.
        /// </summary>
        public ISetupItem Item
        {
            get { return _sortedItem.Item; }
        }


        /// <summary>
        /// Gets the <see cref="ISortedItem{T}"/> of the item.
        /// </summary>
        public ISortedItem<ISetupItem> SortedItem
        {
            get { return _sortedItem; }
        }

        /// <summary>
        /// If <see cref="Item"/> implements <see cref="IVersionedItem"/>, its version is returned (it can be null).
        /// Otherwise, null is returned.
        /// Null has always the same semantics: the item is not versioned.
        /// </summary>
        public Version ItemVersion 
        {
            get 
            {
                IVersionedItem v = Item as IVersionedItem;
                return v != null ? v.Version : null;
            }
        }

        /// <summary>
        /// Whether this driver is the head of a container.
        /// </summary>
        internal abstract bool IsGroupHead { get; }

        /// <summary>
        /// Gets the full name associated to this driver.
        /// It ends with ".Head" if <see cref="IsGroupHead"/> is true.
        /// </summary>
        public readonly string FullName;

        /// <summary>
        /// Gets the current version of the <see cref="Item"/> if it is a <see cref="IVersionedItem"/>. 
        /// Null if the item does not exist yet in the target system or if <see cref="Item"/> is not a <see cref="IVersionedItem"/>.
        /// </summary>
        public readonly VersionedName ExternalVersion;

        /// <summary>
        /// The <see cref="ISetupEngine"/> to which this driver belongs.
        /// </summary>
        public readonly ISetupEngine Engine;

        internal abstract bool ExecuteInit();

        internal abstract bool ExecuteInstall();
        
        internal abstract bool ExecuteSettle();

    }
}
