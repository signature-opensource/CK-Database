using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection.Emit;
using System.Threading;

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

        /// <summary>
        /// Find the common ancestor of all the directory in the list. All the path list MUST be rooted.
        /// Return null if non.
        /// </summary>
        /// <param name="dirlist">List of directory to analyze</param>
        /// <returns>The common full path</returns>
        public static string FindCommonAncestor( IList<string> dirlist )
        {
            var orderedList = dirlist.OrderBy( x => x );
            DirectoryInfo commonDirectory = orderedList.Select( x => new DirectoryInfo( x ) ).FirstOrDefault();
            string common = null;
            while( common == null && commonDirectory != null )
            {
                if( orderedList.All( x => x.StartsWith( commonDirectory.FullName ) ) )
                {
                    common = commonDirectory.FullName;
                }
                else
                {
                    commonDirectory = commonDirectory.Parent;
                }
            }
            return common;
        }

        public static bool Build( IStObjEngineConfiguration config, IActivityLogger logger = null, Action<AppDomain> appDomainHook = null )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            if( logger == null ) logger = DefaultActivityLogger.Empty;

            if( config.StObjBuilderAppDomainConfiguration.UseIndependentAppDomain )
            {
                IList<string> dirPath = new List<string>( config.StObjBuilderAppDomainConfiguration.ProbePaths );

                var result = FindCommonAncestor( dirPath );
                if( result == null )
                    throw new ApplicationException( string.Format( "All the probe paths must have a common ancestor. No ancestor can be found with : {0}", string.Join( "\n", dirPath ) ) );


                AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
                setup.ApplicationBase = result;
                //setup.DisallowApplicationBaseProbing = true; 
                setup.PrivateBinPathProbe = "*";
                setup.PrivateBinPath = string.Join( ";", config.StObjBuilderAppDomainConfiguration.ProbePaths );
                var appdomain = AppDomain.CreateDomain( "StObjContextRoot.Build.IndependentAppDomain", null, setup );
                if( appDomainHook != null ) appDomainHook( appdomain );

                using( Semaphore semaphore = new Semaphore( 0, 1, "local" ) )
                {
                    appdomain.SetData( "config", config );
                    appdomain.SetData( "logger", logger );
                    appdomain.DoCallBack( new CrossAppDomainDelegate( LaunchRunCrossDomain ) );
                    semaphore.WaitOne();
                    return true;
                }
            }
            else
            {
                return LaunchRun( config, logger );
            }
            //return false;
        }

        private static bool LaunchRun( IStObjEngineConfiguration config, IActivityLogger logger )
        {
            IStObjBuilder runner = (IStObjBuilder)Activator.CreateInstance( SimpleTypeFinder.WeakDefault.ResolveType( config.BuilderAssemblyQualifiedName, true ), logger, config );
            runner.Run();
            return true;
        }

        private static void LaunchRunCrossDomain()
        {
            Semaphore semaphore = Semaphore.OpenExisting( "local" );
            try
            {
                LaunchRun( (IStObjEngineConfiguration)AppDomain.CurrentDomain.GetData( "config" ), (IActivityLogger)AppDomain.CurrentDomain.GetData( "logger" ) );
            }
            finally
            {
                if( semaphore != null ) semaphore.Release();
            }
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
