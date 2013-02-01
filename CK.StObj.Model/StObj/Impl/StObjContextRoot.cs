using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection.Emit;

namespace CK.Core
{
    public abstract class StObjContextRoot : IStObjMap
    {
        public static readonly string RootContextTypeName = "CK.StObj.GeneratedRootContext";

        public static IStObjMap Load( string assemblyName, IActivityLogger logger = null )
        {
            return Load( Assembly.Load( assemblyName ), logger );
        }

        public static IStObjMap Load( Assembly a, IActivityLogger logger = null )
        {
            if( logger == null ) logger = DefaultActivityLogger.Empty;
            using( logger.OpenGroup( LogLevel.Info, "Loading dynamic '{0}'", a.FullName ) )
            {
                if( a == null ) throw new ArgumentNullException( "a" );
                Type t = a.GetType( RootContextTypeName, true );
                return (StObjContextRoot)Activator.CreateInstance( t, new object[] { logger } );
            }
        }

        public static IStObjMap LoadOrBuild( IStObjEngineConfiguration config, IActivityLogger logger = null, bool forceBuild = false )
        {
            throw new NotImplementedException();
        }

        public static IStObjMap Build( IStObjEngineConfiguration config, IActivityLogger logger = null )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            if( logger == null ) logger = DefaultActivityLogger.Empty;

            /// 

            object builder = Activator.CreateInstance( SimpleTypeFinder.WeakDefault.ResolveType( config.BuilderAssemblyQualifiedName, true ), logger, config );

            return null;
        }

        readonly StObjContext _defaultContext;
        readonly StObjContext[] _contexts;
        readonly IReadOnlyCollection<StObjContext> _contextsEx;

        internal readonly StructuredObjectCache SingletonCache;
        internal readonly IActivityLogger Logger;
        internal readonly object[] BuilderValues;
        internal readonly StObj[] StObjs;
        internal readonly int SpecializationCount;

        protected StObjContextRoot( IActivityLogger logger, Type[] allTypes )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( allTypes == null ) throw new ArgumentNullException( "allTypes" );
            Logger = logger;
            StObjs = new StObj[allTypes.Length];
            for( int i = 0; i < allTypes.Length; ++i )
            {
                StObjs[i] = new StObj( this, allTypes[i] );
            }
            using( Stream s = GetType().Assembly.GetManifestResourceStream( RootContextTypeName + ".Data" ) )
            {
                BinaryReader reader = new BinaryReader( s );

                _contexts = new StObjContext[reader.ReadInt32()];
                _contextsEx = new ReadOnlyListOnIList<StObjContext>( _contexts );
                _defaultContext = ReadContexts( reader );

                BinaryFormatter formatter = new BinaryFormatter();
                BuilderValues = (object[])formatter.Deserialize( s );
                
                SpecializationCount = reader.ReadInt32();
                SingletonCache = new StructuredObjectCache( SpecializationCount );
                foreach( var o in StObjs )
                {
                    o.Initialize( reader );
                }
                // Singleton creation.
                foreach( var o in StObjs )
                {
                    object instance = SingletonCache.Get(o.CacheIndex);
                    if( instance == null ) SingletonCache.Set( o.CacheIndex, instance = Activator.CreateInstance( o.LeafSpecialization.ObjectType, true ) );
                    o.CallConstruct( logger, idx => SingletonCache.Get( StObjs[idx].CacheIndex ), instance );
                    if( o.Specialization == null ) o.SetPostBuilProperties( idx => SingletonCache.Get( StObjs[idx].CacheIndex ), instance );
                }
            }
        }

        private StObjContext ReadContexts( BinaryReader reader )
        {
            StObjContext def = null;
            for( int i = 0; i < _contexts.Length; ++i )
            {
                string name = reader.ReadString();

                int typeMappingCount = reader.ReadInt32();
                Dictionary<Type,int> mappings = new Dictionary<Type, int>( typeMappingCount );
                while( --typeMappingCount >= 0 )
                {
                    mappings.Add( SimpleTypeFinder.Default.ResolveType( reader.ReadString(), true ), reader.ReadInt32() );
                }
                _contexts[i] = new StObjContext( this, name, mappings );
                if( name.Length == 0 ) def = _contexts[i];
            }
            return def;
        }

        internal StObjContext DoFindContext( string context )
        {
            foreach( var c in _contexts )
            {
                if( c.Context == context ) return c;
            }
            return null;
        }

        public IContextualStObjMap Default
        {
            get { return _defaultContext; }
        }

        public IReadOnlyCollection<IContextualStObjMap> Contexts
        {
            get { return _contextsEx; }
        }

        public IContextualStObjMap FindContext( string context )
        {
            return DoFindContext( context );
        }

    }
}
