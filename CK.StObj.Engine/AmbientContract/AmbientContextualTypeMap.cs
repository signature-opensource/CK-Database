using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace CK.Core
{

    /// <summary>
    ///
    /// </summary>
    public class AmbientContextualTypeMap<T, TC> : IAmbientContextualTypeMap
        where T : AmbientTypeInfo
        where TC : AmbientContextualTypeInfo<T, TC>
    {
        Dictionary<object,TC> _map;
        string _context;
        IAmbientTypeMap _owner;

        internal protected AmbientContextualTypeMap( IAmbientTypeMap owner, string context )
        {
            Debug.Assert( context != null );
            _context = context;
            _map = new Dictionary<object, TC>();
            _owner = owner;
        }

        public IAmbientTypeMap Owner
        {
            get { return _owner; }
        }

        public Dictionary<object, TC> RawMappings 
        { 
            get { return _map; } 
        }

        public string Context
        {
            get { return _context; }
        }

        public int MappedTypeCount 
        { 
            get { return _map.Count; } 
        }

        public Type ToLeafType( Type t )
        {
            TC c = ToLeaf( t );
            return c != null ? c.AmbientTypeInfo.Type : null;
        }

        internal TC ToLeaf( Type t )
        {
            TC c;
            if( _map.TryGetValue( t, out c ) ) return c;
            return null;
        }

        public Type ToHighestImplType( Type t )
        {
            TC c = ToHighestImpl( t );
            return c != null ? c.AmbientTypeInfo.Type : null;
        }

        internal TC ToHighestImpl( Type t )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
            TC c;
            if( _map.TryGetValue( t, out c ) )
            {
                if( c.AmbientTypeInfo.Type != t )
                {
                    if( t.IsInterface )
                    {
                        _map.TryGetValue( new AmbientContractInterfaceKey( t ), out c );
                    }
                    else
                    {
                        while( (c = c.Generalization) != null )
                        {
                            if( c.AmbientTypeInfo.Type == t ) break;
                        }
                    }
                }
            }
            return c;
        }

        public bool IsMapped( Type t )
        {
            return _map.ContainsKey( t );
        }

    }
}
