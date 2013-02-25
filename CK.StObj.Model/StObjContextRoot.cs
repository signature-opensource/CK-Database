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

        public static StObjBuildResult LoadOrBuild( IStObjEngineConfiguration config, IActivityLogger logger = null, bool forceBuild = false )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Find the common ancestor of all the directory in the list. All the path list MUST be rooted.
        /// Return null if non.
        /// </summary>
        /// <param name="dirlist">List of directory to analyze</param>
        /// <returns>The common full path</returns>
        public static string FindCommonAncestor( IEnumerable<string> dirlist )
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

        class AppDomainCommunication : MarshalByRefObject
        {
            readonly object _locker = new object();
            bool _done;
            bool _success;

            public AppDomainCommunication( IActivityLogger logger, IStObjEngineConfiguration config )
            {
                if( !config.GetType().IsSerializable ) throw new InvalidOperationException( "IStObjEngineConfiguration must be serializable." );
                _locker = new object();
                LoggerBridge = new ActivityLoggerBridge( logger );
                Config = config;
            }

            public ActivityLoggerBridge LoggerBridge { get; private set; }

            public IStObjEngineConfiguration Config { get; private set; }

            public bool WaitForResult()
            {
                lock( _locker )
                    while( !_done )
                        Monitor.Wait( _locker );
                return _success;
            }

            public void SetResult( bool success )
            {
                _success = success;
                lock( _locker )
                {
                    _done = true;
                    Monitor.Pulse( _locker );
                }
            }
        }

        public static StObjBuildResult Build( IStObjEngineConfiguration config, IActivityLogger logger = null )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            if( logger == null ) logger = DefaultActivityLogger.Create();

            if( config.AppDomainConfiguration.UseIndependentAppDomain )
            {
                var result = FindCommonAncestor( config.AppDomainConfiguration.ProbePaths );
                if( result == null )
                {
                    throw new CKException( "All the probe paths must have a common ancestor. No ancestor found for: '{0}'.", string.Join( "', '", config.AppDomainConfiguration.ProbePaths ) );
                }
                AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
                setup.ApplicationBase = result;
                // Do not use ApplicationBase, use only Probe paths.
                setup.PrivateBinPathProbe = String.Empty;
                setup.PrivateBinPath = string.Join( ";", config.AppDomainConfiguration.ProbePaths );
                var appDomain = AppDomain.CreateDomain( "StObjContextRoot.Build.IndependentAppDomain", null, setup );
                AppDomainCommunication appDomainComm = new AppDomainCommunication( logger, config );
                appDomain.SetData( "ck-appDomainComm", appDomainComm );
                appDomain.DoCallBack( new CrossAppDomainDelegate( LaunchRunCrossDomain ) );
                return new StObjBuildResult( appDomainComm.WaitForResult(), appDomain, logger );
            }
            else
            {
                return new StObjBuildResult( LaunchRun( logger, config ), null, null );
            }
        }

        private static bool LaunchRun( IActivityLogger logger, IStObjEngineConfiguration config )
        {
            logger.Info( "AppDomain.CurrentDomain.FriendlyName: {0}", AppDomain.CurrentDomain.FriendlyName );
            IStObjBuilder runner = (IStObjBuilder)Activator.CreateInstance( SimpleTypeFinder.WeakDefault.ResolveType( config.BuilderAssemblyQualifiedName, true ), logger, config );
            return runner.Run();
        }

        private static void LaunchRunCrossDomain()
        {
            AppDomain thisDomain = AppDomain.CurrentDomain;
            AppDomainCommunication appDomainComm = (AppDomainCommunication)thisDomain.GetData( "ck-appDomainComm" );
            
            IDefaultActivityLogger logger = DefaultActivityLogger.Create();
            foreach( var item in logger.Output.RegisteredClients.ToArray() )
	        {
                logger.Output.NonRemoveableClients.Remove( item );
                logger.Output.UnregisterClient( item );
	        }
            
            try
            {
                logger.Output.RegisterClient( new ActivityLoggerClientBridge( appDomainComm.LoggerBridge ) );
                appDomainComm.SetResult( LaunchRun( logger, appDomainComm.Config ) );
            }
            catch( Exception ex )
            {
                logger.Fatal( ex );
                appDomainComm.SetResult( false );
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
