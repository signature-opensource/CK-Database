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
    /// Very simple collection of <see cref="ISetupObjectProtoItem"/> indexed by their FullName: duplicate proto 
    /// item (based on their FullName) are not collected. FullName is not tracked: once added to this collector
    /// the FullName of the item must not be changed.
    /// </summary>
    public class SetupObjectItemCollector : IReadOnlyCollection<ISetupObjectProtoItem>
    {
        readonly Dictionary<string,ISetupObjectProtoItem> _items;

        public SetupObjectItemCollector()
        {
            _items = new Dictionary<string, ISetupObjectProtoItem>();
        }

        public bool Add( ISetupObjectProtoItem item )
        {
            if( _items.ContainsKey( item.ContextLocName.FullName ) ) return false;
            _items.Add( item.ContextLocName.FullName, item );
            return true;
        }

        public ISetupObjectProtoItem Find( string fullName )
        {
            return _items.GetValueWithDefault( fullName, null );
        }

        public bool Contains( object item )
        {
            string name = item as string;
            if( name != null ) return _items.ContainsKey( name );
            ISetupObjectProtoItem i = item as ISetupObjectProtoItem;
            return i != null ? _items.ContainsKey( i.ContextLocName.FullName ) : false;
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public IEnumerator<ISetupObjectProtoItem> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
