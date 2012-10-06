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
        readonly IAmbientTypeContextualMapper _typeMappings;
        readonly StObjMapper _owner;

        internal StObjContextualMapper( StObjMapper owner, IAmbientTypeContextualMapper typeMappings )
        {
            _items = new Dictionary<Type, MutableItem>();
            _typeMappings = typeMappings;
            _owner = owner;
            _owner.Add( this );
        }

        public IStObjMapper Owner 
        { 
            get { return _owner; } 
        }

        public Type Context
        {
            get { return _typeMappings.Context; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public IAmbientTypeContextualMapper TypeMappings
        {
            get { return _typeMappings; }
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
                    t = _typeMappings.HighestImplementation( t );
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
