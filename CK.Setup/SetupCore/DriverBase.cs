using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Abstract base class for setup drivers. It exposes the <see cref="Item"/> that must be setup, its current <see cref="ExternalVersion"/> 
    /// if any and the set of <see cref="DirectDependencies"/>.
    /// Only these direct dependencies should be used by a setup driver, even if by using the <see cref="Engine"/> property, the whole list
    /// of drivers is available.
    /// </summary>
    public abstract class DriverBase
    {
        readonly IDependentItem _item;

        class DirectList : IDriverList
        {
            DriverBase[] _drivers;

            public DirectList( DriverBase[] drivers )
            {
                _drivers = drivers;
            }

            public DriverBase this[string fullName]
            {
                get { return _drivers.FirstOrDefault( d => d.Item.FullName == fullName ); }
            }

            public DriverBase this[ IDependentItem item]
            {
                get { return _drivers.FirstOrDefault( d => d.Item == item ); }
            }

            public bool Contains( object item )
            {
                return IndexOf( item ) >= 0;
            }

            public int IndexOf( object item )
            {
                DriverBase d = item as DriverBase;
                return d != null ? Array.IndexOf( _drivers, d ) : -1;
            }

            public DriverBase this[int index]
            {
                get { return _drivers[index]; }
            }

            public int Count
            {
                get { return _drivers.Length; }
            }

            public IEnumerator<DriverBase> GetEnumerator()
            {
                return  ((IEnumerable<DriverBase>)_drivers).GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _drivers.GetEnumerator();
            }

        }

        internal DriverBase( SetupEngine engine, ISortedItem sortedItem, VersionedName externalVersion, IDriverList headDirectDependencies = null )
        {
            Engine = engine;
            _item = sortedItem.Item;
            Index = sortedItem.Index;
            Rank = sortedItem.Rank;
            FullName = sortedItem.FullName;
            ExternalVersion = externalVersion;
            DirectDependencies = headDirectDependencies ?? new DirectList( sortedItem.Requires.Select( d => Engine.AllDrivers[d.FullName] ).OrderBy( d => d.Index ).ToArray() );
        }

        /// <summary>
        /// Gets the item to setup.
        /// This property is often redefined (masked with the new keyword in C#) to expose a more precise associated type.
        /// </summary>
        public IDependentItem Item
        {
            get { return _item; }
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
                IVersionedItem v = _item as IVersionedItem;
                return v != null ? v.Version : v.Version;
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
        /// The position of this driver inside the list of setup drivers.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// The rank of the <see cref="Item"/> in the dependency graph.
        /// </summary>
        public readonly int Rank;

        /// <summary>
        /// The <see cref="SetupEngine"/> to which this driver belongs.
        /// </summary>
        public readonly SetupEngine Engine;

        /// <summary>
        /// Gets the list of drivers that are associated to the direct dependencies
        /// of the <see cref="Item"/>. A driver should interact only with these objects.
        /// </summary>
        public readonly IDriverList DirectDependencies;


        internal abstract bool ExecuteInit();

        internal abstract bool ExecuteInstall();
        
        internal abstract bool ExecuteSettle();

    }
}
