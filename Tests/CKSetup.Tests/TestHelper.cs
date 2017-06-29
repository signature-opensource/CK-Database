using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CK.Core;
using NUnit.Framework;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using System.Data.SqlClient;

namespace CKSetup.Tests
{
    static class TestHelper
    {
        static string _solutionFolder;
        static string _configuration;
        static string _binFolder;
        static string _testOutputPath;
        static IActivityMonitor _monitor;
        static ActivityMonitorConsoleClient _console;
        static SqlConnectionStringBuilder _masterConnectionString;

        static TestHelper()
        {
            _monitor = new ActivityMonitor();
            _monitor.MinimalFilter = LogFilter.Debug;
            _monitor.Output.BridgeTarget.HonorMonitorFilter = false;
            _console = new ActivityMonitorConsoleClient();
            LogsToConsole = true;
        }

        public static IActivityMonitor ConsoleMonitor => _monitor;

        public static bool LogsToConsole
        {
            get { return _monitor.Output.Clients.Contains( _console ); }
            set
            {
                if( value ) _monitor.Output.RegisterUniqueClient( c => c == _console, () => _console );
                else _monitor.Output.UnregisterClient( _console );
            }
        }

        /// <summary>
        /// Gets the connection string to the master database.
        /// It is first the environment variable named "CK_DB_TEST_MASTER_CONNECTION_STRING", then 
        /// the <see cref="AppSettings.Default"/>["CK_DB_TEST_MASTER_CONNECTION_STRING"] in configuration 
        /// file end then, if none are defined, this defaults to "Server=.;Database=master;Integrated Security=SSPI".
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
                    ConsoleMonitor.Info().Send( "Using default connection string: {0}", c );
                }
                _masterConnectionString = new SqlConnectionStringBuilder( c );
            }
            return _masterConnectionString;
        }

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

        public static string BinFolder
        {
            get { if( _binFolder == null ) InitalizePaths(); return _binFolder; }
        }

        public static string Configuration
        {
            get { if( _binFolder == null ) InitalizePaths(); return _configuration; }
        }

        public static string SolutionFolder
        {
            get { if( _solutionFolder == null ) InitalizePaths(); return _solutionFolder; }
        }

        public static string TestFolder
        {
            get { if( _testOutputPath == null ) InitalizePaths(); return _testOutputPath; }
        }

        public static string StObjModel461Path => Path.Combine( SolutionFolder, "CK.StObj.Model", "bin", "Debug", "net461" );
        public static string StObjRuntime461Path => Path.Combine( SolutionFolder, "CK.StObj.Runtime", "bin", "Debug", "net461" );
        public static string StObjEngine461Path => Path.Combine( SolutionFolder, "CK.StObj.Engine", "bin", "Debug", "net461" );

        public static string SetupableModel461Path => Path.Combine( SolutionFolder, "CK.Setupable.Model", "bin", "Debug", "net461" );
        public static string SetupableRuntime461Path => Path.Combine( SolutionFolder, "CK.Setupable.Runtime", "bin", "Debug", "net461" );
        public static string SetupableEngine461Path => Path.Combine( SolutionFolder, "CK.Setupable.Engine", "bin", "Debug", "net461" );

        public static string SqlServerSetupModel461Path => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Model", "bin", "Debug", "net461" );
        public static string SqlServerSetupRuntime461Path => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Runtime", "bin", "Debug", "net461" );
        public static string SqlServerSetupEngine461Path => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Engine", "bin", "Debug", "net461" );

        public static string SqlCallDemoModel461Path => Path.Combine( SolutionFolder, "Tests", "SqlCallDemo", "SqlCallDemo", "bin", "Debug", "net461" );

        public static string SqlActorPackageModel461Path => Path.Combine( SolutionFolder, "Tests", "BasicModels", "SqlActorPackage", "bin", "Debug", "net461" );
        public static string SqlActorPackageRuntime461Path => Path.Combine( SolutionFolder, "Tests", "BasicModels", "SqlActorPackage.Runtime", "bin", "Debug", "net461" );


        #region Net standard component Paths
        public static string StObjModelNet13Path => Path.Combine( SolutionFolder, "CK.StObj.Model", "bin", "Debug", "netstandard1.3" );
        public static string StObjRuntimeNet16Path => Path.Combine( SolutionFolder, "CK.StObj.Runtime", "bin", "Debug", "netstandard1.6" );
        public static string StObjEngineNet16Path => Path.Combine( SolutionFolder, "CK.StObj.Engine", "bin", "Debug", "netstandard1.6" );

        public static string SetupableModelNet13Path => Path.Combine( SolutionFolder, "CK.Setupable.Model", "bin", "Debug", "netstandard1.3" );
        public static string SetupableRuntimeNet16Path => Path.Combine( SolutionFolder, "CK.Setupable.Runtime", "bin", "Debug", "netstandard1.6" );
        public static string SetupableEngineNet16Path => Path.Combine( SolutionFolder, "CK.Setupable.Engine", "bin", "Debug", "netstandard1.6" );

        public static string SqlServerSetupModelNet13Path => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Model", "bin", "Debug", "netstandard1.3" );
        public static string SqlServerSetupRuntimeNet16Path => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Runtime", "bin", "Debug", "netstandard1.6" );
        public static string SqlServerSetupEngineNet16Path => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Engine", "bin", "Debug", "netstandard1.6" );

        public static string SqlCallDemoModelNet13Path => Path.Combine( SolutionFolder, "Tests", "SqlCallDemo", "SqlCallDemo", "bin", "Debug", "netstandard1.3" );


        #endregion

        public static string GetTestZipPath( string suffix = null, [CallerMemberName]string name = null )
        {
            return Path.Combine( TestFolder, name + suffix + ".zip" );
        }

        public static string GetCleanTestZipPath( string suffix = null, [CallerMemberName]string name = null )
        {
            var p = GetTestZipPath( suffix, name );
            File.Delete( p );
            return p;
        }

        static bool _standardDbHasNet461;

        public static ZipRuntimeArchive OpenCKDatabaseZip()
        {
            string zipPath = GetTestZipPath( null, "Standard" );
            ZipRuntimeArchive zip = ZipRuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath );
            if( !_standardDbHasNet461 )
            {
                zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, StObjModel461Path ) ).Should().BeTrue();
                zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, StObjRuntime461Path ) ).Should().BeTrue();
                zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, StObjEngine461Path ) ).Should().BeTrue();
                zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SetupableModel461Path ) ).Should().BeTrue();
                zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SetupableRuntime461Path ) ).Should().BeTrue();
                zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SetupableEngine461Path ) ).Should().BeTrue();
                zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SqlServerSetupModel461Path ) ).Should().BeTrue();
                zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SqlServerSetupRuntime461Path ) ).Should().BeTrue();
                zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SqlServerSetupEngine461Path ) ).Should().BeTrue();
                _standardDbHasNet461 = true;
            }
            #region NetStandard
            //// Net standard
            //if( !_standardDbHasNetStandard && netStandard )
            //{
            //    zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, StObjModelNet13Path ) ).Should().BeTrue();
            //    zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, StObjRuntimeNet16Path ) ).Should().BeTrue();
            //    zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, StObjEngineNet16Path ) ).Should().BeTrue();
            //    zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SetupableModelNet13Path ) ).Should().BeTrue();
            //    zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SetupableRuntimeNet16Path ) ).Should().BeTrue();
            //    zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SetupableEngineNet16Path ) ).Should().BeTrue();
            //    zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SqlServerSetupModelNet13Path ) ).Should().BeTrue();
            //    zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SqlServerSetupRuntimeNet16Path ) ).Should().BeTrue();
            //    zip.AddComponent( CKSetup.BinFolder.ReadBinFolder( ConsoleMonitor, SqlServerSetupEngineNet16Path ) ).Should().BeTrue();
            //    _standardDbHasNetStandard = true;
            //}

            #endregion
            zip.CommitChanges();
            return zip;
        }

        private static void InitalizePaths()
        {
            string p = _binFolder = AppContext.BaseDirectory;
#if DEBUG
            _configuration = "Debug";
#else
            _configuration = "Release";
#endif
            while( !Directory.EnumerateFiles( p ).Where( f => f.EndsWith( ".sln" ) ).Any() )
            {
                p = Path.GetDirectoryName( p );
            }
            _solutionFolder = p;
            _testOutputPath = Path.Combine( SolutionFolder, "Tests", "CKSetup.Tests", "TestOutput" );
            if( Directory.Exists( _testOutputPath ) ) Directory.Delete( _testOutputPath, true );
            Directory.CreateDirectory( _testOutputPath );
            Console.WriteLine( $"SolutionFolder is: {_solutionFolder}." );
            Console.WriteLine( $"Core path: {typeof( string ).GetTypeInfo().Assembly.CodeBase}." );
        }

    }
}
