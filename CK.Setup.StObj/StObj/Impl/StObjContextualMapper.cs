using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class StObjContextualMapper : IStObjContextualMapper
    {
        readonly Dictionary<Type,MutableItem> _items;
        readonly IAmbientTypeContextualMapper _mappings;
        readonly StObjMapper _owner;

        internal StObjContextualMapper( StObjMapper owner, IAmbientTypeContextualMapper mappings )
        {
            _items = new Dictionary<Type, MutableItem>();
            _mappings = mappings;
            _owner = owner;
            _owner.Add( this );
        }

        public IStObjMapper Owner 
        { 
            get { return _owner; } 
        }

        public Type Context
        {
            get { return _mappings.Context; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public IAmbientTypeContextualMapper Mappings
        {
            get { return _mappings; }
        }

        public IStObj this[Type t]
        {
            get { return t != null ? Find( t ) : null; }
        }

        internal void Add( MutableItem item )
        {
            _items.Add( item.ObjectType, item );
        }

        internal MutableItem Find( Type t )
        {
            Debug.Assert( t != null );
            MutableItem r;
            if( !_items.TryGetValue( t, out r ) )
            {
                if( t.IsInterface && typeof( IAmbientContract ).IsAssignableFrom( t ) )
                {
                    t = _mappings.HighestImplementation( t );
                    if( t != null )
                    {
                        _items.TryGetValue( t, out r );
                    }
                }
            }
            return r;
        }
        
        internal ICollection<MutableItem> MutableItems
        {
            get { return _items.Values; }
        }
    }
}
