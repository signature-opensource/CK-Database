#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\ProtoItem\DependentProtoItemCollector.cs) is part of CK-Database. 
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
    /// Very simple collection of <see cref="ISetupItem"/> indexed by their FullName: duplicate  
    /// item (based on their FullName) are not collected. FullName is not tracked: once added to this collector
    /// the FullName of the item must not be changed.
    /// </summary>
    public class SetupItemCollector : IReadOnlyCollection<ISetupItem>
    {
        readonly Dictionary<string, ISetupItem> _items;

        public SetupItemCollector()
        {
            _items = new Dictionary<string, ISetupItem>();
        }

        public bool Add( ISetupItem item )
        {
            if( _items.ContainsKey( item.FullName ) ) return false;
            _items.Add( item.FullName, item );
            return true;
        }

        public ISetupItem Find( string fullName )
        {
            return _items.GetValueWithDefault( fullName, null );
        }

        public bool Contains( object item )
        {
            string name = item as string;
            if( name != null ) return _items.ContainsKey( name );
            ISetupItem i = item as ISetupItem;
            return i != null ? _items.ContainsKey( i.FullName ) : false;
        }

        public int Count => _items.Count; 

        public IEnumerator<ISetupItem> GetEnumerator() => _items.Values.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
