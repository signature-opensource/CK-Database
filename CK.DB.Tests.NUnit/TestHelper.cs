using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CK.Setup;
using CK.SqlServer.Setup;
using NUnit.Framework;
using System.Data.SqlClient;
using System.Reflection;
using CK.SqlServer.Parser;

namespace CK.Core
{
    /// <summary>
    /// Centralized helper functions that offers a Monitor, monitoring Logs initialization
    /// and simple database management.
    /// </summary>
    public static class TestHelper
    {
        static IActivityMonitor _monitor;
        static ActivityMonitorConsoleClient _console;
        static SqlConnectionStringBuilder _masterConnectionString;
        static StObjEngineConfiguration _config;
        static IStObjMap _map;
        static SqlServerParser _sqlParser;

        static string _binFolder;
        static string _projectFolder;
        static string _solutionFolder;
        static string _repositoryFolder;
        static string _logFolder;
        static string _currentTestProjectName;
        static string _buildConfiguration;

        static TestHelper()
        {
        }

        /// <summary>
        /// Gets the monitor.
        /// </summary>
        public static IActivityMonitor Monitor
        {
            get
            {
                if( _monitor == null )
                {
                    LogFile.RootLogPath = LogFolder;
                    _monitor = new ActivityMonitor();
                    _console = new ActivityMonitorConsoleClient();
                }
                return _monitor;
            }
        }

        /// <summary>
        /// Gets or sets whether <see cref="Monitor"/> will log into the console.
        /// Defaults to true.
        /// </summary>
        public static bool LogToConsole
        {
            get { return Monitor.Output.Clients.Contains( _console ); }
            set
            {
                if( LogToConsole != value )
                {
                    if( value )
                    {
                        Monitor.Output.RegisterClient( _console );
                        Monitor.Info( "Switching console log ON." );
                    }
                    else
                    {
                        Monitor.Info( "Switching console log OFF." );
                        Monitor.Output.UnregisterClient( _console );
                    }
                }
            }
        }

        /// <summary>
        /// Gets the path to the log folder. It is the 'Tests/Logs' folder of the solution. 
        /// </summary>
        public static string LogFolder
        {
            get
            {
                if( _logFolder == null ) InitalizePaths();
                return _logFolder;
            }
        }

        /// <summary>
        /// Gets the path to the test project folder.
        /// This is usually where files and folders specific to the test should be (like a
        /// "TestScripts" folder).
        /// </summary>
        public static string TestProjectFolder
        {
            get
            {
                if( _projectFolder == null ) InitalizePaths();
                return _projectFolder;
            }
        }

        /// <summary>
        /// Gets the path to the root folder: where the .git folder is.
        /// </summary>
        public static string RepositoryFolder
        {
            get
            {
                if( _repositoryFolder == null ) InitalizePaths();
                return _repositoryFolder;
            }
        }

        /// <summary>
        /// Gets the default IStObjMap (after having executed a db setup via RunDBSetup() 
        /// if it is not already available: the setup is done only once).
        /// </summary>
        public static IStObjMap StObjMap
        {
            get
            {
                if( _map == null )
                {
                    Assert.That( RunDBSetup() );
                }
                return _map;
            }
        }

        /// <summary>
        /// Loads the <see cref="StObjMap"/> from existing generated assembly.
        /// Loading is done only if StObjMap is not already available.
        /// </summary>
        /// <returns>The map or null if an error occurred.</returns>
        public static IStObjMap LoadStObjMapFromExistingGeneratedAssembly()
        {
            if( _map == null )
            {
                using( Monitor.OpenInfo( "Loading StObj map from generated assembly." ) )
                {
                    try
                    {
                        string assemblyName = Config.GeneratedAssemblyName;
                        var a = LoadAssemblyFromAppContextBaseDirectory( assemblyName );
                        _map = StObjContextRoot.Load( a, StObjContextRoot.DefaultStObjRuntimeBuilder, Monitor );
                    }
                    catch( Exception ex )
                    {
                        Monitor.Error( ex );
                    }
                }
            }
            return _map;
        }

        /// <summary>
        /// Gets a shared reusable <see cref="SqlServerParser"/>.
        /// </summary>
        static public SqlServerParser SqlServerParser => _sqlParser ?? ( _sqlParser = new SqlServerParser());

        /// <summary>
        /// Gets the solution folder. It is the parent directory of the 'Tests/' folder (that must exist).
        /// </summary>
        static public string SolutionFolder
        {
            get
            {
                if( _solutionFolder == null ) InitalizePaths();
                return _solutionFolder;
            }
        }

        /// <summary>
        /// Gets the bin folder where the tests are beeing executed.
        /// </summary>
        static public string BinFolder
        {
            get
            {
                if( _binFolder == null ) InitalizePaths();
                return _binFolder;
            }
        }

        /// <summary>
        /// Runs the database setup based on <see cref="Config"/> and updates <see cref="StObjMap"/>.
        /// Automatically called by StObjMap when the StObjMap is not yet initialized.
        /// </summary>
        /// <param name="traceStObjGraphOrdering">True to trace input and output of StObj graph ordering.</param>
        /// <param name="traceSetupGraphOrdering">True to trace input and output of setup graph ordering.</param>
        /// <param name="revertNames">True to revert names in ordering.</param>
        public static bool RunDBSetup( bool traceStObjGraphOrdering = false, bool traceSetupGraphOrdering = false, bool revertNames = false )
        {
            using( Monitor.OpenTrace( $"Running Setup on {DatabaseTestConnectionString}." ) )
            {
                try
                {
                    Config.RevertOrderingNames = revertNames;
                    Config.TraceDependencySorterInput = traceStObjGraphOrdering;
                    Config.TraceDependencySorterOutput = traceStObjGraphOrdering;

                    var setupable = Config.Aspects.OfType<SetupableAspectConfiguration>().Single();
                    setupable.RevertOrderingNames = revertNames;
                    setupable.TraceDependencySorterInput = traceSetupGraphOrdering;
                    setupable.TraceDependencySorterOutput = traceSetupGraphOrdering;
                    bool success = RunStObjEngine( Config );
                    if( success )
                    {
                        success = LoadStObjMapFromExistingGeneratedAssembly() != null;
                    }
                    return success;
                }
                catch( Exception ex )
                {
                    Monitor.Error( ex );
                    throw;
                }
            }
        }

        /// <summary>
        /// Runs a StObjEngine with a standard "weak assembly resolver".
        /// </summary>
        /// <param name="c">The configuration. Must not be null.</param>
        /// <returns>True on success, false on error.</returns>
        public static bool RunStObjEngine( StObjEngineConfiguration c )
        {
            return WithWeakAssemblyResolver( () =>
            {
                var e = new StObjEngine( Monitor, c );
                return e.Run();
            } );
        }

        /// <summary>
        /// Runs code inside a standard "weak assembly resolver".
        /// </summary>
        /// <param name="action">The action. Must not be null.</param>
        /// <returns>The action result.</returns>
        public static T WithWeakAssemblyResolver<T>( Func<T> action )
        {
            if( action == null ) throw new ArgumentNullException( nameof( action ) );
            ResolveEventHandler loadHook = ( sender, arg ) =>
            {
                var failed = new AssemblyName( arg.Name );
                var resolved = failed.Version != null && string.IsNullOrWhiteSpace( failed.CultureName )
                        ? Assembly.Load( new AssemblyName( failed.Name ) )
                        : null;
                Monitor.Info( $"[CK.DB.Tests.NUnit]Load conflict: {arg.Name} => {(resolved != null ? resolved.FullName : "(null)")}" );
                return resolved;
            };
            AppDomain.CurrentDomain.AssemblyResolve += loadHook;
            try
            {
                return action();
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= loadHook;
            }
        }


        /// <summary>
        /// Loads an assembly that must be in probe paths in .Net framework and in
        /// AppContext.BaseDirectory in .Net Core.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly to load (without any .dll suffix).</param>
        /// <returns>The loaded assembly.</returns>
        static public Assembly LoadAssemblyFromAppContextBaseDirectory( string assemblyName )
        {
//#if NET461
            return Assembly.Load( new AssemblyName( assemblyName ) );
//#else
//            return Assembly.LoadFrom( Path.Combine( AppContext.BaseDirectory, assemblyName + ".dll" ) );
//#endif
        }


        /// <summary>
        /// Clears all <see cref="UsedSchemas"/> and resets <see cref="StObjMap"/> (using <see cref="DatabaseTestConnectionString"/>
        /// by default).
        /// </summary>
        public static void ClearDatabaseUsedSchemas( string connectionSting = null )
        {
            connectionSting = connectionSting ?? DatabaseTestConnectionString;
            var monitor = TestHelper.Monitor;
            using( monitor.OpenInfo( $"Clearing used schemas ({connectionSting})." ) )
            using( var m = new SqlManager( monitor ) )
            {
                m.OpenFromConnectionString( connectionSting );
                var schemas = TestHelper.UsedSchemas;
                int maxTryCount = schemas.Count;
                bool retry;
                do
                {
                    retry = false;
                    foreach( var s in schemas )
                    {
                        if( s == "CKCore" )
                        {
                            TestHelper.Monitor.Trace( "Removing 'CKCore' objets." );
                            retry |= !m.SchemaDropAllObjects( "CKCore", false );
                        }
                        else
                        {
                            TestHelper.Monitor.Trace( $"Removing '{s}' schema and its objets." );
                            retry |= !m.SchemaDropAllObjects( s, true );
                        }
                    }
                }
                while( --maxTryCount >= 0 && retry );
                if( retry ) throw new CKException( "Unable to clear all schemas." );
            }
            _map = null;
        }

        /// <summary>
        /// Gets the connection string to the master database.
        /// It is first the environment variable named "CK_DB_TEST_MASTER_CONNECTION_STRING", then 
        /// the <see cref="AppSettings.Default"/>["CK_DB_TEST_MASTER_CONNECTION_STRING"] in configuration 
        /// file end then, if none are defined, this defaults to "Server=.;Database=master;Integrated Security=SSPI".
        /// </summary>
        public static string MasterConnectionString => EnsureMasterConnection().ToString();

        static SqlConnectionStringBuilder EnsureMasterConnection()
        {
            if( _masterConnectionString == null )
            {
                string c = Environment.GetEnvironmentVariable( "CK_DB_TEST_MASTER_CONNECTION_STRING" );
                if( c == null ) c = AppSettings.Default["CK_DB_TEST_MASTER_CONNECTION_STRING"];
                if( c == null )
                {
                    c = "Server=.;Database=master;Integrated Security=SSPI";
                    Monitor.Info( $"Using default connection string: {c}" );
                }
                _masterConnectionString = new SqlConnectionStringBuilder( c );
            }
            return _masterConnectionString;
        }

        /// <summary>
        /// Gets the database test name from AppSettings.Default["DatabaseTestName"] (typically from configuration file).
        /// Note that the <see cref="AppSettings.Default"/> may be <see cref="AppSettings.Override(Func{Func{string, object}, string, object})">overridden</see>
        /// by code.
        /// </summary>
        public static string DatabaseTestName
        {
            get
            {
                var dbName = AppSettings.Default["DatabaseTestName"];
                if( dbName == null )
                {
                    throw new Exception( "AppSettings.Default[\"DatabaseTestName\"] is null. Defines <appSettings><add key=\"DatabaseTestName\" value=\"XXXX\" /></appSettings> in App.config file." );
                }
                return dbName;
            }
        }

        /// <summary>
        /// Gets the connection string to the <see cref="DatabaseTestName"/>.
        /// </summary>
        public static string DatabaseTestConnectionString => GetConnectionString( DatabaseTestName );

        /// <summary>
        /// Gets the connection string based on <see cref="MasterConnectionString"/> to the given database.
        /// </summary>
        /// <param name="dbName">Name of the database.</param>
        /// <returns>The connection string to the database.</returns>
        public static string GetConnectionString( string dbName )
        {
            var c = EnsureMasterConnection();
            string savedMaster = c.InitialCatalog;
            c.InitialCatalog = dbName;
            string result = c.ToString();
            c.InitialCatalog = savedMaster;
            return result;
        }

        /// <summary>
        /// Gets the assemblies to test from configuration file application settings (comma separated names from "AssembliesToSetup" key).
        /// </summary>
        public static IReadOnlyList<string> AssembliesToSetup
        {
            get
            {
                var c = AppSettings.Default["AssembliesToSetup"];
                if( c == null ) c = string.Empty;
                return c.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                        .Select( s => s.Trim() ).ToArray();
            }
        }

        /// <summary>
        /// Gets the schema names used. This is read from configuration file application settings (comma separated names from "UsedSchemas" key).
        /// The "CKCore" and "CK" schemas are automatically added to this list.
        /// </summary>
        public static IReadOnlyList<string> UsedSchemas
        {
            get
            {
                var c = AppSettings.Default["UsedSchemas"];
                if( c == null ) c = string.Empty;
                return c.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                        .Select( s => s.Trim() )
                        .Append( "CK" ).Append( "CKCore" )
                        .Distinct()
                        .ToArray();
            }
        }

        /// <summary>
        /// Gets the assembly name that will be emitted from configuration file application settings (first "GeneratedAssemblyName" key).
        /// </summary>
        public static string GeneratedAssemblyName => AppSettings.Default["GeneratedAssemblyName"];

        /// <summary>
        /// Gets or sets the build configuration (Debug/Release).
        /// Default to the this CK.DB.Tests.NUnit configuration build.
        /// </summary>
        public static string BuildConfiguration
        {
            get
            {
                if( _solutionFolder == null )
                {
                    InitalizePaths();
                }
                return _buildConfiguration;
            }
        }

        public static string CurrentTestProjectName
        {
            get
            {
                if( _solutionFolder == null ) InitalizePaths();
                return _currentTestProjectName;
            }
        }


        /// <summary>
        /// Gets the configuration that <see cref="StObjMap"/> will use.
        /// This configuration uses <see cref="GeneratedAssemblyName"/>, <see cref="AssembliesToSetup"/>
        /// and <see cref="DatabaseTestConnectionString"/> by default.
        /// </summary>
        public static StObjEngineConfiguration Config
        {
            get
            {
                if( _config == null )
                {
                    _config = new StObjEngineConfiguration();
                    foreach( var a in AssembliesToSetup )
                    {
                        _config.Assemblies.Add( a );
                    }
                    _config.GeneratedAssemblyName = GeneratedAssemblyName;

                    var cSetupable = new SetupableAspectConfiguration();
                    _config.Aspects.Add( cSetupable );

                    var c = new SqlSetupAspectConfiguration();
                    c.DefaultDatabaseConnectionString = DatabaseTestConnectionString;
                    c.IgnoreMissingDependencyIsError = true; // Set to true while we don't have SqlFragment support.

                    _config.Aspects.Add( c );
                }
                return _config;
            }
        }

        static void InitalizePaths()
        {
#if DEBUG
            _buildConfiguration = "Debug";
#else
            _buildConfiguration = "Release";
#endif
            string p = _binFolder = AppContext.BaseDirectory;
            string altConfDir = _buildConfiguration == "Release" ? "Debug" : "Release";
            string buildConfDir = FindAbove( p, _buildConfiguration ) ?? FindAbove( p, altConfDir );
            if( buildConfDir == null )
            {
                throw new InvalidOperationException( $"Unable to find parent folder named '{_buildConfiguration}' or '{altConfDir}' above '{_binFolder}'. Please explicitly set TestHelper.BuildConfiguration property." );
            }
            p = Path.GetDirectoryName( buildConfDir );
            if( Path.GetFileName( p ) != "bin" )
            {
                throw new InvalidOperationException( $"Folder '{_buildConfiguration}' MUST be in 'bin' folder (above '{_binFolder}')." );
            }
            _projectFolder = p = Path.GetDirectoryName( p );
            _currentTestProjectName = Path.GetFileName( p );
            Assembly entry = Assembly.GetEntryAssembly();
            if( entry != null )
            {
                string assemblyName = entry.GetName().Name;
                if( _currentTestProjectName != assemblyName )
                {
                    throw new InvalidOperationException( $"Current test project assembly is '{assemblyName}' but folder is '{_currentTestProjectName}' (above '{_buildConfiguration}' in '{_binFolder}')." );
                }
            }
            p = Path.GetDirectoryName( p );

            string testsFolder = null;
            bool hasGit = false;
            while( p != null && !(hasGit = Directory.Exists( Path.Combine( p, ".git" ) )) )
            {
                if( Path.GetFileName( p ) == "Tests" ) testsFolder = p;
                p = Path.GetDirectoryName( p );
            }
            if( !hasGit ) throw new InvalidOperationException( $"The project must be in a git repository (above '{_binFolder}')." );
            _repositoryFolder = p;
            if( testsFolder == null )
            {
                throw new InvalidOperationException( $"A parent 'Tests' folder must exist above '{_projectFolder}'." );
            }
            _solutionFolder = Path.GetDirectoryName( testsFolder );
            _logFolder = Path.Combine( testsFolder, "Logs" );
        }

        static string FindAbove( string path, string folderName )
        {
            while( path != null && Path.GetFileName( path ) != folderName )
            {
                path = Path.GetDirectoryName( path );
            }
            return path;
        }

    }
}
