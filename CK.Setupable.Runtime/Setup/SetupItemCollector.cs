#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\ProtoItem\DependentProtoItemCollector.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Collections.Generic;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Very simple collection of <see cref="ISetupItem"/> indexed by their FullName: duplicate  
    /// item (based on their FullName) are not collected. FullName is not tracked: once added to this collector
    /// the FullName of the item must not be changed.
    /// </summary>
    public class SetupItemCollector : IReadOnlyCollection<ISetupItem>
    {
        readonly Dictionary<string, ISetupItem> _items;

        /// <summary>
        /// Initializes a new <see cref="SetupItemCollector"/>.
        /// </summary>
        public SetupItemCollector()
        {
            _items = new Dictionary<string, ISetupItem>();
        }

        /// <summary>
        /// Adds a setup item. If an item with the same <see cref="ISetupItem.FullName"/>
        /// is already registered, it is ignored.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>True if it has been added, false otherwise.</returns>
        public bool Add( ISetupItem item )
        {
            if( _items.ContainsKey( item.FullName ) ) return false;
            _items.Add( item.FullName, item );
            return true;
        }

        /// <summary>
        /// Finds an item by its full name or null if not found.
        /// </summary>
        /// <param name="fullName">The item name to find.</param>
        /// <returns>The setup item or null.</returns>
        public ISetupItem Find( string fullName )
        {
            return _items.GetValueWithDefault( fullName, null );
        }

        /// <summary>
        /// Looks up by name (a string) or by <see cref="ISetupItem.FullName"/>.
        /// </summary>
        /// <param name="item">The name or the item.</param>
        /// <returns>True if found, false otherwise.</returns>
        public bool Contains( object item )
        {
            string name = item as string;
            if( name != null ) return _items.ContainsKey( name );
            ISetupItem i = item as ISetupItem;
            return i != null ? _items.ContainsKey( i.FullName ) : false;
        }

        /// <summary>
        /// Gets the number of items in this collector.
        /// </summary>
        public int Count => _items.Count; 

        /// <summary>
        /// Gets the <see cref="ISetupItem"/>.
        /// </summary>
        /// <returns>An enumerator of the items.</returns>
        public IEnumerator<ISetupItem> GetEnumerator() => _items.Values.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
