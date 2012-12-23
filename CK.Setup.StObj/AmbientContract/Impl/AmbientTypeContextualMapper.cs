using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Core
{

    /// <summary>
    /// Internal wrapper for keys in <see cref="AmbientTypeContextualMapper"/>: when wrapped in this class,
    /// the Type is the key of its highest implementation instead of its final concrete class.
    /// This enables the use of one and only one dictionnary for Mappings (Type => Final Type) as well as 
    /// highest implementation association (Ambient contract interface => its highest implementation).
    /// </summary>
    internal class AmbientContractInterfaceKey
    {
        public readonly Type InterfaceType;

        public AmbientContractInterfaceKey( Type ambientContractInterface )
        {
            Debug.Assert( typeof(IAmbientContract).IsAssignableFrom( ambientContractInterface ) );
            InterfaceType = ambientContractInterface;
        }

        public override bool Equals( object obj )
        {
            AmbientContractInterfaceKey k = obj as AmbientContractInterfaceKey;
            return k != null && k.InterfaceType == InterfaceType;
        }

        public override int GetHashCode()
        {
            return -InterfaceType.GetHashCode();
        }
    }

    /// <summary>
    /// Internal implementation of <see cref="IAmbientTypeContextualMapper"/> exposed by <see cref="IAmbientTypeMapper"/>.
    /// </summary>
    internal class AmbientTypeContextualMapper<T,TC> : IAmbientTypeContextualMapper
        where T : AmbientTypeInfo
        where TC : AmbientContextTypeInfo<T>
    {
        Dictionary<object,TC> _map;
        string _context;
        AmbientTypeMapper _owner;

        internal AmbientTypeContextualMapper( AmbientTypeMapper owner, string context, Dictionary<object, TC> m )
        {
            Debug.Assert( context != null );
            _context = context;
            _map = m;
            _owner = owner;
            _owner.Add( this );
        }

        public IAmbientTypeMapper Owner
        {
            get { return _owner; }
        }

        public string Context
        {
            get { return _context; }
        }

        public Type this[Type t]
        {
            get 
            {
                TC ctxType;
                if( _map.TryGetValue( t, out ctxType ) ) return ctxType.AmbientTypeInfo.Type; 
                return null; 
            }
        }

        public Type HighestImplementation( Type ambientContractInterface )
        {
            if( ambientContractInterface == null ) throw new ArgumentNullException( "ambientContractInterface" );
            if( !ambientContractInterface.IsInterface || !typeof( IAmbientContract ).IsAssignableFrom( ambientContractInterface ) )
            {
                throw new ArgumentException( "Must be an interface that specializes IAmbientContract.", "ambientContractInterface" );
            }
            TC ctxType;
            if( _map.TryGetValue( new AmbientContractInterfaceKey( ambientContractInterface ), out ctxType ) ) return ctxType.AmbientTypeInfo.Type;
            return null;
        }

        public Type HighestImplementation<TInterface>() where TInterface : class, IAmbientContract
        {
            if( !typeof( TInterface ).IsInterface ) throw new ArgumentException( "Must be the type of an interface.", "T" );
            TC ctxType;
            if( _map.TryGetValue( new AmbientContractInterfaceKey( typeof( TInterface ) ), out ctxType ) ) return ctxType.AmbientTypeInfo.Type;
            return null;
        }

        public int Count { get { return _map.Count; } }

        public bool IsMapped( Type t )
        {
            return _map.ContainsKey( t );
        }

    }
}
