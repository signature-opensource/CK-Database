using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public class ProtoItemCollector
    {
        Dictionary<string,List<IDependentProtoItem>> _items;

        public ProtoItemCollector()
        {
            _items = new Dictionary<string, List<IDependentProtoItem>>();
        }

        public void Add( IDependentProtoItem item )
        {
            EnsureList( item.FullName ).Add( item );
        }

        public IList<IDependentProtoItem> FindList( string fullName )
        {
            return _items.GetValueWithDefault( fullName, null );
        }

        public IList<IDependentProtoItem> EnsureList( string fullName )
        {
            List<IDependentProtoItem> list;
            if( !_items.TryGetValue( fullName, out list ) )
            {
                list = new List<IDependentProtoItem>();
                _items.Add( fullName, list );
            }
            return list;
        }

        
    }
}
