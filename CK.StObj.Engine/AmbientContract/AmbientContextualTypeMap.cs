#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AmbientContract\AmbientContextualTypeMap.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace CK.Core
{

    /// <summary>
    /// Concrete base implementation for a <see cref="IContextualTypeMap"/>.
    /// </summary>
    public class AmbientContextualTypeMap<T, TC> : IContextualTypeMap
        where T : AmbientTypeInfo
        where TC : AmbientContextualTypeInfo<T, TC>
    {
        Dictionary<object,TC> _map;
        string _context;
        IContextualRoot<IContextualTypeMap> _owner;

        /// <summary>
        /// Initializes a new <see cref="AmbientContextualTypeMap{T,TC}"/>.
        /// </summary>
        /// <param name="owner">The root context.</param>
        /// <param name="context">Name of this context.</param>
        internal protected AmbientContextualTypeMap( IContextualRoot<IContextualTypeMap> owner, string context )
        {
            Debug.Assert( context != null );
            _context = context;
            _map = new Dictionary<object, TC>();
            _owner = owner;
        }

        /// <summary>
        /// Gets all the contexts including this one.
        /// </summary>
        public IContextualRoot<IContextualTypeMap> AllContexts => _owner; 

        /// <summary>
        /// Gets the mappings between types (base types as well as ambient contract interfaces) to objects. 
        /// </summary>
        public Dictionary<object, TC> RawMappings  => _map; 

        /// <summary>
        /// Gets this context name.
        /// </summary>
        public string Context => _context; 

        /// <summary>
        /// Gets the number of existing mappings.
        /// </summary>
        public int MappedTypeCount  => _map.Count; 

        /// <summary>
        /// Gets the final mapped type for any type that is mapped.
        /// </summary>
        /// <param name="t">Base type.</param>
        /// <returns>Most specialized type or null if not found.</returns>
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

        /// <summary>
        /// Gets the most abstract type for any type mapped.
        /// </summary>
        /// <param name="t">Any mapped type.</param>
        /// <returns>The most abstract, less specialized, associated type.</returns>
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

        /// <summary>
        /// Gets whether a type is mapped.
        /// </summary>
        /// <param name="t">Any type.</param>
        /// <returns>True if the type is mapped.</returns>
        public bool IsMapped( Type t ) => _map.ContainsKey( t );

        /// <summary>
        /// Gets all types mapped by this contextual map.
        /// </summary>
        public IEnumerable<Type> Types
        {
            get { return _map.Keys.Select( o => o is Type ? (Type)o : ((AmbientContractInterfaceKey)o).InterfaceType ); }
        }
    }
}
