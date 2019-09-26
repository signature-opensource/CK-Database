using System;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Abstract base class for setup drivers. 
    /// It exposes the <see cref="Item"/> that must be setup, its current <see cref="ExternalVersion"/> if any.
    /// </summary>
    /// <remarks>
    /// This class is not intended to be specialized outside this CK.Setupable.Runtime assembly: it is used as a base 
    /// class for the actual item driver (<see cref="SetupItemDriver"/>) and by an internal class of the CK.Setupable.Engine assembly
    /// for handling heads of groups or containers.
    /// </remarks>
    public abstract class DriverBase
    {
        readonly ISortedItem<ISetupItem> _sortedItem;

        internal DriverBase( IDriverList drivers, IDriverBaseList allDrivers, ISortedItem<ISetupItem> sortedItem, VersionedName externalVersion )
        {
            _sortedItem = sortedItem;
            ExternalVersion = externalVersion;
            FullName = _sortedItem.FullName;
            Drivers = drivers;
            AllDriverBase = allDrivers;
        }

        /// <summary>
        /// Gets the item to setup.
        /// This property is often redefined (masked with the new keyword in C#) to expose a more precise associated type.
        /// </summary>
        public ISetupItem Item => _sortedItem.Item; 

        /// <summary>
        /// Gets the <see cref="ISortedItem{T}"/> of the item.
        /// </summary>
        public ISortedItem<ISetupItem> SortedItem => _sortedItem;

        /// <summary>
        /// If <see cref="Item"/> implements <see cref="IVersionedItem"/>, its version is returned (it can be null).
        /// Otherwise, null is returned.
        /// Null has always the same semantics: the item is not versioned.
        /// </summary>
        public Version ItemVersion => (_sortedItem.Item as IVersionedItem)?.Version;

        /// <summary>
        /// Gets the ordered list of <see cref="SetupItemDriver"/> indexed by the <see cref="IDependentItem.FullName"/> 
        /// or by the <see cref="IDependentItem"/> object instance itself.
        /// </summary>
        public IDriverList Drivers { get; }

        /// <summary>
        /// Gets the ordered list of <see cref="DriverBase"/> indexed by the <see cref="IDependentItem.FullName"/> 
        /// or by the <see cref="IDependentItem"/> object instance itself that participate to Setup.
        /// This list contains all the <see cref="SetupItemDriver"/> plus all the internal drivers for the head of Groups 
        /// or Containers (the ones that are not SetupItemDriver instances and have a <see cref="DriverBase.FullName"/> that
        /// ends with ".Head").
        /// </summary>
        public IDriverBaseList AllDriverBase { get; }

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

        internal abstract bool ExecuteInit( IActivityMonitor monitor );

        internal abstract bool ExecuteInstall( IActivityMonitor monitor );
        
        internal abstract bool ExecuteSettle( IActivityMonitor monitor );

    }
}
