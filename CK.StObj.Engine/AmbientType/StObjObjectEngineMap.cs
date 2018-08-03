using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using CK.Setup;

namespace CK.Core
{

    /// <summary>
    /// Mutable implementation of <see cref="IStObjTypeMap"/> that handles <see cref="MutableItem"/>
    /// </summary>
    class StObjObjectEngineMap : IStObjTypeMap
    {
        readonly Dictionary<object, MutableItem> _map;
        readonly MutableItem[] _allSpecializations;

        /// <summary>
        /// Initializes a new <see cref="StObjObjectEngineMap"/>.
        /// </summary>
        internal protected StObjObjectEngineMap( MutableItem[] allSpecializations )
        {
            _map = new Dictionary<object, MutableItem>();
            _allSpecializations = allSpecializations;
        }

        internal void AddClassMapping( Type t, MutableItem m )
        {
            Debug.Assert( t.IsClass );
            _map.Add( t, m );
        }

        internal void AddInterfaceMapping( Type t, MutableItem m, MutableItem finalType )
        {
            Debug.Assert( t.IsInterface );
            _map.Add( t, finalType );
            _map.Add( new AmbientContractInterfaceKey( t ), m );
        }

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
            MutableItem c = ToLeaf( t );
            return c != null ? c.Type.Type : null;
        }

        internal MutableItem ToLeaf( Type t )
        {
            _map.TryGetValue( t, out var c );
            return c;
        }

        /// <summary>
        /// Gets all the specialization. If there is no error, this list corresponds to the
        /// last items of the <see cref="AmbientContractCollectorResult.ConcreteClasses"/>.
        /// </summary>
        internal IReadOnlyCollection<MutableItem> AllSpecializations => _allSpecializations;

        /// <summary>
        /// Gets all the mapping from object (including <see cref="AmbientContractInterfaceKey"/>) to
        /// <see cref="MutableItem"/>.
        /// </summary>
        internal IEnumerable<KeyValuePair<object, MutableItem>> RawMappings => _map;

        /// <summary>
        /// Gets the most abstract type for any type mapped.
        /// </summary>
        /// <param name="t">Any mapped type.</param>
        /// <returns>The most abstract, less specialized, associated type.</returns>
        public Type ToHighestImplType( Type t )
        {
            MutableItem c = ToHighestImpl( t );
            return c != null ? c.Type.Type : null;
        }

        internal MutableItem ToHighestImpl( Type t )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
            MutableItem c;
            if( _map.TryGetValue( t, out c ) )
            {
                if( c.Type.Type != t )
                {
                    if( t.GetTypeInfo().IsInterface )
                    {
                        _map.TryGetValue( new AmbientContractInterfaceKey( t ), out c );
                    }
                    else
                    {
                        while( (c = c.Generalization) != null )
                        {
                            if( c.Type.Type == t ) break;
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
