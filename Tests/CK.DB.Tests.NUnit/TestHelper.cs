using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CK.Setup;
using CK.SqlServer.Setup;
using NUnit.Framework;
using System.Data.SqlClient;

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
        static SetupEngineConfiguration _config;
        static IStObjMap _map;
        static string _binFolder;
        static string _projectFolder;
        static string _solutionFolder;
        static string _logFolder;

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
                    SystemActivityMonitor.RootLogPath = LogFolder;
                    CK.Monitoring.GrandOutput.EnsureActiveDefaultWithDefaultSettings();
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
                        Monitor.Info().Send( "Switching console log ON." );
                    }
                    else
                    {
                        Monitor.Info().Send( "Switching console log OFF." );
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
        /// Gets the path to the project folder.
        /// </summary>
        public static string ProjectFolder
        {
            get
            {
                if( _projectFolder == null ) InitalizePaths();
                return _projectFolder;
            }
        }

        /// <summary>
        /// Gets the default IStObjMap after having executed a <see cref="RunDBSetup"/>.
        /// The setup is done only once.
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
        /// Gets the solution folder. It must be a git working folder (a '.git' directory must exist) and
        /// contain a 'Tests' folder.
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
        /// Automatically called by StObjMap when the StObjMap is not yet intialized.
        /// </summary>
        /// <param name="traceStObjGraphOrdering">True to trace input and output of StObj graph ordering.</param>
        /// <param name="traceSetupGraphOrdering">True to trace input and output of setup graph ordering.</param>
        /// <param name="revertNames">True to revert names in ordering.</param>
        public static bool RunDBSetup( bool traceStObjGraphOrdering = false, bool traceSetupGraphOrdering = false, bool revertNames = false )
        {
            using( Monitor.OpenTrace().Send( "Running Setup on {0}.", TestHelper.DatabaseTestConnectionString ) )
            {
                try
                {
                    Config.RunningMode = revertNames ? SetupEngineRunningMode.DefaultWithRevertOrderingNames : SetupEngineRunningMode.Default;
                    Config.StObjEngineConfiguration.TraceDependencySorterInput = traceStObjGraphOrdering;
                    Config.StObjEngineConfiguration.TraceDependencySorterOutput = traceStObjGraphOrdering;
                    Config.TraceDependencySorterInput = traceSetupGraphOrdering;
                    Config.TraceDependencySorterOutput = traceSetupGraphOrdering;
                    using( var r = StObjContextRoot.Build( Config, null, TestHelper.Monitor ) )
                    {
                        _map = StObjContextRoot.Load( Config.StObjEngineConfiguration.FinalAssemblyConfiguration.AssemblyName, StObjContextRoot.DefaultStObjRuntimeBuilder, Monitor );
                        return r.Success;
                    }
                }
                catch( Exception ex )
                {
                    Monitor.Error().Send( ex );
                    throw;
                }
            }
        }

        /// <summary>
        /// Clears all <see cref="UsedSchemas"/> and resets <see cref="StObjMap"/> (using <see cref="DatabaseTestConnectionString"/>
        /// by default).
        /// </summary>
        public static void ClearDatabaseUsedSchemas( string connectionSting = null )
        {
            connectionSting = connectionSting ?? DatabaseTestConnectionString;
            var monitor = TestHelper.Monitor;
            using( monitor.OpenInfo().Send( "Clearing used schemas ({0}).", connectionSting ) )
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
                            TestHelper.Monitor.Trace().Send( "Removing 'CKCore' objets." );
                            retry |= !m.SchemaDropAllObjects( "CKCore", false );
                        }
                        else
                        {
                            TestHelper.Monitor.Trace().Send( "Removing '{0}' schema and its objets.", s );
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
        /// file end then, if none are defined, this defaults to "Server=(local)\\NIMP;Database=master;Integrated Security=SSPI".
        /// </summary>
        public static string ConnectionStringMaster => EnsureMasterConnection().ToString();

        static SqlConnectionStringBuilder EnsureMasterConnection()
        {
            if( _masterConnectionString == null )
            {
                string c = Environment.GetEnvironmentVariable( "CK_DB_TEST_MASTER_CONNECTION_STRING" );
                if( c == null ) c = AppSettings.Default["CK_DB_TEST_MASTER_CONNECTION_STRING"];
                if( c == null )
                {
                    c = "Server=.;Database=master;Integrated Security=SSPI";
                    Monitor.Info().Send( "Using default connection string: {0}", c );
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
        /// Gets the connection string based on <see cref="ConnectionStringMaster"/> to the given database.
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
                if( c == null ) c = String.Empty;
                return c.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                        .Select( s => s.Trim() ).ToReadOnlyList();
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
                if( c == null ) c = String.Empty;
                return c.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                        .Select( s => s.Trim() )
                        .Append( "CK" ).Append( "CKCore" )
                        .Distinct()
                        .ToReadOnlyList();
            }
        }

        /// <summary>
        /// Gets the assembly name that will be emitted from configuration file application settings (first "DynamicAssemblyName" key).
        /// </summary>
        public static string DynamicAssemblyName => AppSettings.Default["DynamicAssemblyName"];

        /// <summary>
        /// Gets the configuration that <see cref="StObjMap"/> will use.
        /// This configuration uses <see cref="DynamicAssemblyName"/>, <see cref="AssembliesToSetup"/>
        /// and <see cref="DatabaseTestConnectionString"/> by default.
        /// </summary>
        public static SetupEngineConfiguration Config
        {
            get
            {
                if( _config == null )
                {
                    _config = new SetupEngineConfiguration();
                    _config.StObjEngineConfiguration.BuildAndRegisterConfiguration.UseIndependentAppDomain = true;
                    _config.StObjEngineConfiguration.FinalAssemblyConfiguration.GenerateFinalAssemblyOption = BuilderFinalAssemblyConfiguration.GenerateOption.GenerateFileAndPEVerify;
                    foreach( var a in AssembliesToSetup )
                    {
                        _config.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.Add( a );
                    }
                    _config.StObjEngineConfiguration.FinalAssemblyConfiguration.AssemblyName = DynamicAssemblyName;

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
            string p = new Uri( System.Reflection.Assembly.GetExecutingAssembly().CodeBase ).LocalPath;
            // Code base is like "...HumanSide\Tests\CK.ActorModel.Tests\Debug\bin\CK.ActorModel.Tests.dll"
            _binFolder = p = Path.GetDirectoryName( p );
            _projectFolder = p = Path.GetDirectoryName( Path.GetDirectoryName( p ) );

            bool hasGit = false;
            while( p != null && !(hasGit = Directory.Exists( Path.Combine( p, ".git" ) )) )
            {
                p = Path.GetDirectoryName( p );
            }
            if( !hasGit ) throw new InvalidOperationException( "The project must be in a git repository." );

            _solutionFolder = p;
            p = Path.Combine( p, "Tests" );
            if( !Directory.Exists( p ) )
            {
                throw new InvalidOperationException( "The solution must contain a 'Tests' folder." );
            }

            _logFolder = Path.Combine( p, "Logs" );
        }
    }
}
