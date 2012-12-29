using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CK.Core
{
    public class StObjContextRoot : IContextRoot
    {
        public static readonly string RootContextTypeName = "CK.StObj.GeneratedRootContext";

        public static IContextRoot Load( string assemblyName )
        {
            return Load( Assembly.Load( assemblyName ) );
        }

        public static IContextRoot Load( Assembly a )
        {
            if( a == null ) throw new ArgumentNullException( "a" );
            Type t = a.GetType( RootContextTypeName, true );
            return (IContextRoot)Activator.CreateInstance( t );
        }

        #region IContextRoot Members

        readonly IContextualObjectMap _defaultContext;
        readonly IContextualObjectMap[] _contexts;
        readonly IReadOnlyCollection<IContextualObjectMap> _contextsEx;
        readonly Type[] _finalTypes;
        readonly object[] _finalObjects;

        protected StObjContextRoot()
        {
            using( Stream s = GetType().Assembly.GetManifestResourceStream( RootContextTypeName + ".Data" ) )
            {
                BinaryReader reader = new BinaryReader( s );
                BinaryFormatter formatter = new BinaryFormatter();

                _finalTypes = (Type[])formatter.Deserialize( s );
                _finalObjects = new object[_finalTypes.Length];
                for( int i = 0; i < _finalTypes.Length; ++i )
                {
                    _finalObjects[i] = Activator.CreateInstance( _finalTypes[i], true );
                }
                int nbContext = reader.ReadInt32();
                _contexts = new IContextualObjectMap[nbContext];
                _contextsEx = new ReadOnlyListOnIList<IContextualObjectMap>( _contexts );
                for( int i = 0; i < nbContext; ++i )
                {
                    string name = reader.ReadString();

                    Dictionary<Type,int> mappings = new Dictionary<Type, int>();
                    {
                        var all = (Tuple<Type, int>[])formatter.Deserialize( s );
                        foreach( var e in all ) mappings.Add( e.Item1, e.Item2 );
                    }
                    Dictionary<Type,Type> highestImpl = new Dictionary<Type, Type>();
                    {
                        var all = (Tuple<Type, Type>[])formatter.Deserialize( s );
                        foreach( var e in all ) highestImpl.Add( e.Item1, e.Item2 );
                    }
                    _contexts[i] = new ContextMap( this, name, mappings, highestImpl );
                    if( name.Length == 0 ) _defaultContext = _contexts[i];
                }
            }
        }

        class ContextMap : IContextualObjectMap
        {
            readonly StObjContextRoot _root;
            readonly string _name;
            readonly Dictionary<Type,int> _mappings;
            readonly Dictionary<Type,Type> _highestImpl;

            internal ContextMap( StObjContextRoot root, string name, Dictionary<Type, int> mappings, Dictionary<Type, Type> highestImpl )
            {
                _root = root;
                _name = name;
                _mappings = mappings;
                _highestImpl = highestImpl;
            }

            public string Context
            {
                get { return _name; }
            }

            public int MappedTypeCount
            {
                get { return _mappings.Count; }
            }

            public Type MapType( Type t )
            {
                int idx;
                if( _mappings.TryGetValue( t, out idx ) )
                {
                    return _root._finalTypes[idx];
                }
                return null;
            }

            public Type HighestImplementation( Type ambientContractInterface )
            {
                if( ambientContractInterface == null ) throw new ArgumentNullException( "ambientContractInterface" );
                if( !ambientContractInterface.IsInterface || !typeof( IAmbientContract ).IsAssignableFrom( ambientContractInterface ) )
                {
                    throw new ArgumentException( "Must be an interface that specializes IAmbientContract.", "ambientContractInterface" );
                }
                return _highestImpl.GetValueWithDefault( ambientContractInterface, null );
            }

            public Type HighestImplementation<TInterface>() where TInterface : class, IAmbientContract
            {
                Type t = typeof( TInterface );
                if( !t.IsInterface ) throw new ArgumentException( "Must be the type of an interface.", "T" );
                return _highestImpl.GetValueWithDefault( t, null );
            }

            public bool IsMapped( Type t )
            {
                return _mappings.ContainsKey( t );
            }

            public object Obtain( Type t )
            {
                int idx;
                if( _mappings.TryGetValue( t, out idx ) )
                {
                    return _root._finalObjects[idx];
                }
                return null;
            }

            public T Obtain<T>() where T : class
            {
                return (T)Obtain( typeof( T ) );
            }

        }

        IContextualObjectMap IContextRoot.Default
        {
            get { return _defaultContext; }
        }

        IReadOnlyCollection<IContextualObjectMap> IContextRoot.Contexts
        {
            get { return _contextsEx; }
        }

        IContextualObjectMap IContextRoot.FindContext( string context )
        {
            foreach( var c in _contexts )
            {
                if( c.Context == context ) return c;
            }
            return null;
        }

        #endregion
    }
}
