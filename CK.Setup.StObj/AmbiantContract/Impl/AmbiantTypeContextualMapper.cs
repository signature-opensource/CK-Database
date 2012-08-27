using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Core
{

    /// <summary>
    /// Internal wrapper for keys in <see cref="AmbiantTypeContextualMapper"/>: when wrapped in this class,
    /// the Type is the key of its highest implementation instead of its final concrete class.
    /// This enables the use of one and only one dictionnary for Mappings (Type => Final Type) as well as 
    /// highest implementation association (Ambiant contract interface => its highest implementation).
    /// </summary>
    internal class AmbiantContractInterfaceKey
    {
        public readonly Type InterfaceType;

        public AmbiantContractInterfaceKey( Type ambiantContractInterface )
        {
            Debug.Assert( typeof(IAmbiantContract).IsAssignableFrom( ambiantContractInterface ) );
            InterfaceType = ambiantContractInterface;
        }

        public override bool Equals( object obj )
        {
            AmbiantContractInterfaceKey k = obj as AmbiantContractInterfaceKey;
            return k != null && k.InterfaceType == InterfaceType;
        }

        public override int GetHashCode()
        {
            return -InterfaceType.GetHashCode();
        }
    }

    /// <summary>
    /// Internal implementation of <see cref="IAmbiantTypeContextualMapper"/> exposed by <see cref="IAmbiantTypeMapper"/>.
    /// </summary>
    internal class AmbiantTypeContextualMapper : IAmbiantTypeContextualMapper
    {
        Dictionary<object,Type> _map;
        Type _context;
        AmbiantTypeMapper _owner;

        internal AmbiantTypeContextualMapper( AmbiantTypeMapper owner, Type context, Dictionary<object, Type> m )
        {
            _context = context;
            _map = m;
            _owner = owner;
            _owner.Add( this );
        }

        public IAmbiantTypeMapper Owner
        {
            get { return _owner; }
        }

        public Type Context
        {
            get { return _context; }
        }

        public Type this[Type t]
        {
            get { return _map.GetValueWithDefault( t, null ); }
        }

        public Type HighestImplementation( Type ambiantContractInterface )
        {
            if( ambiantContractInterface == null ) throw new ArgumentNullException( "ambiantContractInterface" );
            if( !ambiantContractInterface.IsInterface || !typeof( IAmbiantContract ).IsAssignableFrom( ambiantContractInterface ) )
            {
                throw new ArgumentException( "Must be an interface that specializes IAmbiantContract.", "ambiantContractInterface" );
            }
            return _map.GetValueWithDefault( new AmbiantContractInterfaceKey( ambiantContractInterface ), null ); 
        }

        public Type HighestImplementation<T>() where T : class, IAmbiantContract
        {
            if( !typeof( T ).IsInterface ) throw new ArgumentException( "Must be the type of an interface.", "T" );
            return _map.GetValueWithDefault( new AmbiantContractInterfaceKey( typeof( T ) ), null );
        }

        public int Count { get { return _map.Count; } }

        public bool IsMapped( Type t )
        {
            return _map.ContainsKey( t );
        }

    }
}
