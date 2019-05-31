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
    /// Internal mutable implementation of <see cref="IStObjObjectEngineMap"/> that handles <see cref="MutableItem"/>.
    /// The internal participants have write access to it. I'm not proud of this (there are definitly cleaner
    /// ways to organize this) but it work...
    /// The map is instanciated by AmbientTypeCollector.GetAmbientObjectResult and then
    /// then internally exposed by the AmbientObjectCollectorResult so that AmbientTypeCollector.GetAmbientServiceResult(AmbientObjectCollectorResult)
    /// can use (and fill) it.
    /// </summary>
    partial class StObjObjectEngineMap : IStObjObjectEngineMap, IStObjMap, IStObjServiceMap
    {
        readonly Dictionary<object, MutableItem> _map;
        readonly MutableItem[] _allSpecializations;

        /// <summary>
        /// Initializes a new <see cref="StObjObjectEngineMap"/>.
        /// </summary>
        /// <param name="mapName">The final map name.</param>
        /// <param name="allSpecializations">
        /// Predimensioned array that will be filled with actual
        /// mutable items by <see cref="StObjCollector.GetResult()"/>.
        /// </param>
        /// <param name="typeKindDetector">The type kind detector.</param>
        internal protected StObjObjectEngineMap(
            string mapName,
            MutableItem[] allSpecializations,
            AmbientTypeKindDetector typeKindDetector )
        {
            Debug.Assert( mapName != null );
            MapName = mapName;
            _map = new Dictionary<object, MutableItem>();
            _allSpecializations = allSpecializations;
            _serviceMap = new Dictionary<Type, AmbientServiceClassInfo>();
            _exposedServiceMap = new ServiceMapTypeAdapter( _serviceMap );
            _serviceManualMap = new Dictionary<Type, IStObjServiceFinalManualMapping>();
            _exposedManualServiceMap = new ServiceManualMapTypeAdapter( _serviceManualMap );
            _serviceManualList = new List<IStObjServiceFinalManualMapping>();
            _typeKindDetector = typeKindDetector;
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
        /// This map auto implements the root <see cref="IStObjMap"/>.
        /// </summary>
        IStObjObjectMap IStObjMap.StObjs => this;

        /// <summary>
        /// Gets the map name. Never null.
        /// </summary>
        public string MapName { get; }

        /// <summary>
        /// Gets the number of existing mappings (the <see cref="RawMappings"/>.Count).
        /// </summary>
        internal int MappedTypeCount => _map.Count;

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
        /// last items of the <see cref="AmbientObjectCollectorResult.ConcreteClasses"/>.
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
        public Type ToHighestImplType( Type t ) => ToHighestImpl( t ).Type.Type;

        internal MutableItem ToHighestImpl( Type t )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
            MutableItem c;
            if( _map.TryGetValue( t, out c ) )
            {
                if( c.Type.Type != t )
                {
                    if( t.IsInterface )
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
        /// Gets the most abstract mapped StObj for a type.
        /// See <see cref="ToHighestImplType(Type)"/>.
        /// </summary>
        /// <param name="t">Any mapped type.</param>
        /// <returns>The most abstract, less specialized, associated StObj.</returns>
        public IStObjResult ToStObj( Type t ) => ToHighestImpl( t );

        /// <summary>
        /// Gets whether a type is mapped.
        /// </summary>
        /// <param name="t">Any type.</param>
        /// <returns>True if the type is mapped.</returns>
        public bool IsMapped( Type t ) => _map.ContainsKey( t );

        public object Obtain( Type t ) => ToLeaf( t )?.InitialObject;

        /// <summary>
        /// Gets all types mapped by this contextual map.
        /// </summary>
        public IEnumerable<Type> Types => _map.Keys.OfType<Type>(); 

        IEnumerable<object> IStObjObjectMap.Implementations => _allSpecializations.Select( m => m.InitialObject );

        public IEnumerable<StObjImplementation> StObjs
        {
            get
            {
                return _map.Where( kv => kv.Key is Type )
                            .Select( kv => new StObjImplementation( kv.Value, kv.Value.InitialObject ) );
            }
        }

        IEnumerable<KeyValuePair<Type, object>> IStObjObjectMap.Mappings
        {
            get
            {
                return _map.Where( kv => kv.Key is Type )
                            .Select( kv => new KeyValuePair<Type, object>( (Type)kv.Key, kv.Value.InitialObject ) );
            }
        }

        IStObjResult IStObjObjectEngineMap.ToLeaf( Type t ) => ToLeaf( t );

        IStObj IStObjObjectMap.ToLeaf( Type t ) => ToLeaf( t );
    }
}
