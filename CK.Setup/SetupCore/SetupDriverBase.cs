using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Base class for setup drivers. It exposes the <see cref="Item"/> that must be setup, its current <see cref="ExternalVersion"/> 
    /// if any and the set of <see cref="DirectDependencies"/>.
    /// Only these direct dependencies should be used by a setup driver, even if by using the <see cref="Engine"/>, the whole list
    /// of drivers is available.
    /// </summary>
    public abstract class SetupDriverBase
    {
        readonly ISetupableItem _item;

        class DirectList : ISetupDriverList
        {
            SetupDriverBase[] _drivers;

            public DirectList( SetupDriverBase[] drivers )
            {
                _drivers = drivers;
            }

            public SetupDriverBase this[string fullName]
            {
                get { return _drivers.FirstOrDefault( d => d.Item.FullName == fullName ); }
            }

            public bool Contains( object item )
            {
                return IndexOf( item ) >= 0;
            }

            public int IndexOf( object item )
            {
                SetupDriverBase d = item as SetupDriverBase;
                return d != null ? Array.IndexOf( _drivers, d ) : -1;
            }

            public SetupDriverBase this[int index]
            {
                get { return _drivers[index]; }
            }

            public int Count
            {
                get { return _drivers.Length; }
            }

            public IEnumerator<SetupDriverBase> GetEnumerator()
            {
                return  ((IEnumerable<SetupDriverBase>)_drivers).GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _drivers.GetEnumerator();
            }

        }

        internal SetupDriverBase( SetupEngine engine, ISortedItem sortedItem, VersionedName externalVersion, ISetupDriverList directDependencies )
        {
            Engine = engine;
            _item = (ISetupableItem)sortedItem.Item;
            Index = sortedItem.Index;
            Rank = sortedItem.Rank;
            FullName = sortedItem.FullName;
            ExternalVersion = externalVersion;
            DirectDependencies = directDependencies ?? new DirectList( sortedItem.Requires.Select( d => Engine.AllDrivers[d] ).OrderBy( d => d.Index ).ToArray() );
        }

        /// <summary>
        /// Whether this driver is the head of a container.
        /// </summary>
        public abstract bool IsContainerHead { get; }

        /// <summary>
        /// Gets the full name associated to this driver.
        /// It ends with ".Head" if <see cref="IsContainerHead"/> is true.
        /// </summary>
        public readonly string FullName;

        /// <summary>
        /// Gets the current version of the <see cref="Item"/>. 
        /// Null if the item does not exist yet in the target system.
        /// The index of this driver in the whole list of driver objects.
        /// </summary>
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
        public readonly ISetupDriverList DirectDependencies;

        /// <summary>
        /// Gets the item to setup.
        /// You may redefine (hiding property) this to expose a more precise associated type.
        /// </summary>
        public ISetupableItem Item
        {
            get { return _item; }
        }

        internal abstract bool ExecuteInit();

        internal abstract bool ExecuteInstall();
        
        internal abstract bool ExecuteSettle();
    }
}
