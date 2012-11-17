using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public class DependentProtoItemCollector : IReadOnlyCollection<IDependentProtoItem>
    {
        readonly Dictionary<string,IDependentProtoItem> _items;

        public DependentProtoItemCollector()
        {
            _items = new Dictionary<string, IDependentProtoItem>();
        }

        public bool Add( IDependentProtoItem item )
        {
            if( _items.ContainsKey( item.FullName ) ) return false;
            _items.Add( item.FullName, item );
            return true;
        }

        public IDependentProtoItem Find( string fullName )
        {
            return _items.GetValueWithDefault( fullName, null );
        }

        public bool Contains( object item )
        {
            string name = item as string;
            if( name != null ) return _items.ContainsKey( name );
            IDependentProtoItem i = item as IDependentProtoItem;
            return i != null ? _items.ContainsKey( i.FullName ) : false;
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public IEnumerator<IDependentProtoItem> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
