using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection.Emit;
using System.Threading;
using System.Diagnostics;

namespace CK.Core
{
    public abstract class StObjContextRoot : IStObjMap
    {
        /// <summary>
        /// Holds the name of the root class.
        /// </summary>
        public static readonly string RootContextTypeName = "CK.StObj.GeneratedRootContext";

        static readonly HashSet<Assembly> _alreadyLoaded = new HashSet<Assembly>();

        /// <summary>
        /// Loads a previously generated assembly by its assembly name.
        /// </summary>
        /// <param name="a">Assembly name that will be loaded in the current AppDomain.</param>
        /// <param name="logger">Optional logger for loading operation.</param>
        /// <returns>A <see cref="IStObjMap"/> that provides access to the objects graph.</returns>
        public static IStObjMap Load( string assemblyName, IActivityLogger logger = null )
        {
            return Load( Assembly.Load( assemblyName ), logger );
        }

        /// <summary>
        /// Loads a previously generated assembly.
        /// </summary>
        /// <param name="a">Assembly (loaded in the current AppDomain).</param>
        /// <param name="logger">Optional logger for loading operation.</param>
        /// <returns>A <see cref="IStObjMap"/> that provides access to the objects graph.</returns>
        public static IStObjMap Load( Assembly a, IActivityLogger logger = null )
        {
            if( logger == null ) logger = DefaultActivityLogger.Empty;
            bool loaded;
            lock( _alreadyLoaded ) 
            {
                loaded = _alreadyLoaded.Contains( a );
                if( !loaded ) _alreadyLoaded.Add( a );
            }
            using( loaded ? null : logger.OpenGroup( LogLevel.Info, "Loading dynamic '{0}'", a.FullName ) )
            {
                if( a == null ) throw new ArgumentNullException( "a" );
                Type t = a.GetType( RootContextTypeName, true );
                return (StObjContextRoot)Activator.CreateInstance( t, new object[] { logger } );
            }
        }

        /// <summary>
        /// Find the common ancestor of all the directory in the list. All the path list MUST be rooted.
        /// Return null if non.
        /// </summary>
        /// <param name="dirlist">List of directory to analyze</param>
        /// <returns>The common full path</returns>
        public static string FindCommonAncestor( IList<string> dirlist )
        {
            int maxLen;
            if( dirlist == null || dirlist.Count == 0 || (maxLen = dirlist[0].Length) == 0 ) return null;
            if( dirlist.Count == 1 ) return dirlist[0];
            Char cU1 = Char.ToUpperInvariant( dirlist[0][0] );
            for( int i = 1; i < dirlist.Count; ++i )
            {
                int l = dirlist[i].Length;
                if( l == 0 ) return null;
                if( maxLen > l ) maxLen = l;
                if( Char.ToUpperInvariant( dirlist[i][0] ) != cU1 ) return null;
            }
            // To be continued with an external loop from 1 to maxLen (to catch chars) and an internal one from 0 to dirlist.Count (for each strings).

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

        enum AppDomainMode
        {
            ForceBuild,
            BuildIfRequired,
            GetVersionStamp,
            ResultFoundExisting
        }

        class AppDomainCommunication : MarshalByRefObject
        {
            readonly object _locker = new object();
            bool _done;
            bool _success;

            public AppDomainCommunication( IActivityLogger logger, IStObjEngineConfiguration config, AppDomainMode m )
            {
                if( !config.GetType().IsSerializable ) throw new InvalidOperationException( "IStObjEngineConfiguration must be serializable." );
                _locker = new object();
                LoggerBridge = logger.Output.ExternalInput;
                Config = config;
                if( m != AppDomainMode.GetVersionStamp ) VersionStampRead = config.FinalAssemblyConfiguration.ExternalVersionStamp;
                Mode = m;
            }

            public string VersionStampRead { get; set; }

            public AppDomainMode Mode { get; set; }

            public ActivityLoggerBridgeTarget LoggerBridge { get; private set; }

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

        /// <summary>
        /// Runs a build based on the given serializable <paramref name="config"/> object. 
        /// The returned <see cref="StObjBuildResult"/> must be disposed once done with it.
        /// </summary>
        /// <param name="config">Configuration object. It must be serializable.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A disposable result.</returns>
        public static StObjBuildResult Build( IStObjEngineConfiguration config, IActivityLogger logger = null, bool forceBuild = false )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            if( logger == null ) logger = new ActivityLogger();

            StObjBuildResult r = null;
            if( config.AppDomainConfiguration.UseIndependentAppDomain && !config.AppDomainConfiguration.Assemblies.IsEmptyConfiguration )
            {
                using( logger.OpenGroup( LogLevel.Info, "Build process. Creating an independant AppDomain." ) )
                {
                    r = BuildOrGetVersionStampInIndependentAppDomain( config, logger, forceBuild ? AppDomainMode.ForceBuild : AppDomainMode.BuildIfRequired );
                }
            }
            else
            {
                if( !forceBuild && config.FinalAssemblyConfiguration.ExternalVersionStamp != null )
                {
                    using( logger.OpenGroup( LogLevel.Info, "Checking potentially existing generated dll ExternalVersionStamp in an independant AppDomain." ) )
                    {
                        // Extracts the Version stamp of the existing dll (if any) in an independent AppDomain to
                        // avoid cluttering the ReflectionOnly context of the current AppDomain.
                        r = BuildOrGetVersionStampInIndependentAppDomain( config, logger, AppDomainMode.GetVersionStamp );
                        if( !r.Success || r.ExternalVersionStamp != config.FinalAssemblyConfiguration.ExternalVersionStamp )
                        {
                            logger.Info( "Build is required." );
                            r.Dispose();
                            r = null;
                        }
                        else
                        {
                            logger.Info( "Generated dll exist with the exact Version stamp. Building it again is useless." );
                        }
                    }
                }
                if( r == null ) r = new StObjBuildResult( LaunchRun( logger, config ), config.FinalAssemblyConfiguration.ExternalVersionStamp, false, null, null );
            }
            return r;
        }

        private static bool LaunchRun( IActivityLogger logger, IStObjEngineConfiguration config )
        {
            logger.Info( "Current AppDomain.CurrentDomain.FriendlyName = '{0}'.", AppDomain.CurrentDomain.FriendlyName );
            IStObjBuilder runner = (IStObjBuilder)Activator.CreateInstance( SimpleTypeFinder.WeakDefault.ResolveType( config.BuilderAssemblyQualifiedName, true ), logger, config );
            return runner.Run();
        }

        private static StObjBuildResult BuildOrGetVersionStampInIndependentAppDomain( IStObjEngineConfiguration config, IActivityLogger logger, AppDomainMode m )
        {
            AppDomainSetup thisSetup = AppDomain.CurrentDomain.SetupInformation;
            AppDomainSetup setup = new AppDomainSetup();

            if( m == AppDomainMode.GetVersionStamp )
            {
                setup.ApplicationBase = thisSetup.ApplicationBase;
                setup.PrivateBinPathProbe = thisSetup.PrivateBinPathProbe;
                setup.PrivateBinPath = thisSetup.PrivateBinPath;
            }
            else
            {
                var result = FindCommonAncestor( config.AppDomainConfiguration.ProbePaths );
                if( result == null )
                {
                    throw new CKException( "All the probe paths must have a common ancestor. No ancestor found for: '{0}'.", string.Join( "', '", config.AppDomainConfiguration.ProbePaths ) );
                }
                setup.ApplicationBase = result;
                /// PrivateBinPathProbe (from msdn):
                /// Set this property to any non-null string value, including String.Empty (""), to exclude the application directory path — that is, 
                /// ApplicationBase — from the search path for the application, and to search for assemblies only in PrivateBinPath. 
                setup.PrivateBinPathProbe = String.Empty;
                setup.PrivateBinPath = string.Join( ";", config.AppDomainConfiguration.ProbePaths );
            }
            var appDomain = AppDomain.CreateDomain( "StObjContextRoot.Build.IndependentAppDomain", null, setup );
            AppDomainCommunication appDomainComm = new AppDomainCommunication( logger, config, m );
            appDomain.SetData( "CK-AppDomainComm", appDomainComm );
            appDomain.DoCallBack( new CrossAppDomainDelegate( LaunchRunCrossDomain ) );
            return new StObjBuildResult( appDomainComm.WaitForResult(), appDomainComm.VersionStampRead, appDomainComm.Mode == AppDomainMode.ResultFoundExisting, appDomain, logger );
        }

        private static void LaunchRunCrossDomain()
        {
            AppDomainCommunication appDomainComm = (AppDomainCommunication)AppDomain.CurrentDomain.GetData( "CK-AppDomainComm" );
            var config = appDomainComm.Config;
            IActivityLogger logger = new ActivityLogger();
            try
            {
                logger.Output.RegisterClient( new ActivityLoggerBridge( appDomainComm.LoggerBridge ) );
                string existingVersionStamp = null;
                if( appDomainComm.Mode == AppDomainMode.GetVersionStamp
                    || (appDomainComm.Mode == AppDomainMode.BuildIfRequired && config.FinalAssemblyConfiguration.ExternalVersionStamp != null) )
                {
                    // If no directory has been specified for final assembly. Trying to use the path of CK.StObj.Model assembly.
                    // If no assembly name has been specified for final assembly. Using default name.
                    // ==> This mimics GenerateFinalAssembly behavior.
                    string directory = config.FinalAssemblyConfiguration.Directory;
                    if( String.IsNullOrEmpty( directory ) ) directory = BuilderFinalAssemblyConfiguration.GetFinalDirectory( directory );
                    string assemblyName = config.FinalAssemblyConfiguration.AssemblyName;
                    if( String.IsNullOrEmpty( assemblyName ) ) assemblyName = BuilderFinalAssemblyConfiguration.GetFinalAssemblyName( assemblyName );

                    string p = Path.Combine( directory, assemblyName + ".dll" );
                    try
                    {
                        if( File.Exists( p ) )
                        {
                            Assembly a = Assembly.ReflectionOnlyLoadFrom( p );
                            foreach( var attr in a.GetCustomAttributesData() )
                            {
                                if( typeof( AssemblyInformationalVersionAttribute ).IsAssignableFrom( attr.Constructor.DeclaringType ) )
                                {
                                    if( attr.ConstructorArguments.Count > 0 ) existingVersionStamp = attr.ConstructorArguments[0].Value as string;
                                    break;
                                }
                            }
                            if( appDomainComm.Mode == AppDomainMode.BuildIfRequired )
                            {
                                if( existingVersionStamp == config.FinalAssemblyConfiguration.ExternalVersionStamp )
                                {
                                    logger.Info( "File '{0}' already exists with the expected Version stamp. Building it again is useless.", p );
                                    appDomainComm.Mode = AppDomainMode.ResultFoundExisting;
                                }
                                else logger.Trace( "File '{0}' already exists but Version stamp differs. Building is required.", p );
                            }
                            else if( appDomainComm.Mode == AppDomainMode.GetVersionStamp )
                            {
                                if( existingVersionStamp != null )
                                {
                                    logger.Info( "File '{0}' already exists. Its Version stamp has been extracted ('{1}').", p, existingVersionStamp );
                                    appDomainComm.Mode = AppDomainMode.ResultFoundExisting;
                                }
                            }
                            appDomainComm.VersionStampRead = existingVersionStamp;
                        }
                        else logger.Trace( "File '{0}' does not exist.", p );
                    }
                    catch( Exception ex )
                    {
                        logger.Error( ex, "While trying to read version stamp from '{0}'.", p );
                    }
                }
                // Conclusion: if a build is required, run it, otherwise if a version has been read, it is a success.
                if( appDomainComm.Mode == AppDomainMode.ResultFoundExisting || appDomainComm.Mode == AppDomainMode.GetVersionStamp )
                {
                    appDomainComm.SetResult( existingVersionStamp != null );
                }
                else
                {
                    // Updates the VersionStampRead on the output.
                    appDomainComm.VersionStampRead = config.FinalAssemblyConfiguration.ExternalVersionStamp;
                    appDomainComm.SetResult( LaunchRun( logger, config ) );
                }
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
                _contextsEx = new CKReadOnlyListOnIList<StObjContext>( _contexts );
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
