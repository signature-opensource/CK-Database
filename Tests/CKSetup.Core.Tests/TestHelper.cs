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
using System.Diagnostics;
using System.Net.Http;
using System.Threading;

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
            LogToConsole = false;
        }

        public static IActivityMonitor Monitor => _monitor;

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

        public static IDisposable EnsureConsoleMonitor()
        {
            bool prev = LogToConsole;
            LogToConsole = true;
            return Util.CreateDisposableAction( () => LogToConsole = prev );
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
                }
                Monitor.Info( $"Master connection string: {c}" );
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

        static HttpClient _sharedHttpClient;

        public static HttpClient SharedHttpClient => _sharedHttpClient ?? (_sharedHttpClient = new HttpClient());

        public static Uri EnsureCKSetupRemoteRunning()
        {
            using( EnsureConsoleMonitor() )
            {
                var workingDir = Path.Combine( SolutionFolder, "CKSetupRemoteStore" );
                var fName = Path.Combine( workingDir, "bin", Configuration, "net461", "CKSetupRemoteStore.exe" );
                var pI = new ProcessStartInfo()
                {
                    WorkingDirectory = workingDir,
                    FileName = fName,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process p = null;
                try
                {
                    p = Process.Start( pI );
                }
                catch( Exception ex )
                {
                    Monitor.Fatal( $"While launching '{fName}' in '{workingDir}'.", ex );
                    throw;
                }
                var u = new Uri( "http://localhost:2982" );
                int tryCount = 0;
                retry:
                ++tryCount;
                try
                {
                    HttpResponseMessage msg;
                    do
                    {
                        msg = SharedHttpClient.GetAsync( u ).GetAwaiter().GetResult();
                    }
                    while( !msg.IsSuccessStatusCode );
                    return u;

                }
                catch( Exception ex )
                {
                    Monitor.Error( $"While geting '{u}' (try n°{tryCount}).", ex );
                    if( p.HasExited )
                    {
                        Monitor.Fatal( "CKSetupRemoteStore.exe has exited." );
                    }
                    else if( tryCount < 5 )
                    {
                        Thread.Sleep( 300 );
                        goto retry;
                    }
                    throw;
                }
            }
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

        // The CKSetup application itself is in net461 & netcorapp2.0
        public static string CKSetupAppNet461 => Path.Combine( SolutionFolder, "CKSetup", "bin", Configuration, "net461" );
        public static string CKSetupAppNetCoreApp20 => Path.Combine( SolutionFolder, "CKSetup", "bin", Configuration, "netcoreapp2.0" );

        // The CK.StObj.Runner is running in net461 & netcorapp2.0
        public static string StObjRunnerNet461 => Path.Combine( SolutionFolder, "CK.StObj.Runner", "bin", Configuration, "net461" );
        public static string StObjRunnerNetCoreApp20 => Path.Combine( SolutionFolder, "CK.StObj.Runner", "bin", Configuration, "netcoreapp2.0" );

        // Net461: the bin folder is enough for all the components.
        public static string StObjModel461 => Path.Combine( SolutionFolder, "CK.StObj.Model", "bin", Configuration, "net461" );
        public static string StObjRuntime461 => Path.Combine( SolutionFolder, "CK.StObj.Runtime", "bin", Configuration, "net461" );
        public static string StObjEngine461 => Path.Combine( SolutionFolder, "CK.StObj.Engine", "bin", Configuration, "net461" );

        public static string SetupableModel461 => Path.Combine( SolutionFolder, "CK.Setupable.Model", "bin", Configuration, "net461" );
        public static string SetupableRuntime461 => Path.Combine( SolutionFolder, "CK.Setupable.Runtime", "bin", Configuration, "net461" );
        public static string SetupableEngine461 => Path.Combine( SolutionFolder, "CK.Setupable.Engine", "bin", Configuration, "net461" );

        public static string SqlServerSetupModel461 => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Model", "bin", Configuration, "net461" );
        public static string SqlServerSetupRuntime461 => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Runtime", "bin", Configuration, "net461" );
        public static string SqlServerSetupEngine461 => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Engine", "bin", Configuration, "net461" );

        public static string SqlCallDemo461 => Path.Combine( SolutionFolder, "Tests", "SqlCallDemo", "SqlCallDemo", "bin", Configuration, "net461" );

        public static string SqlActorPackageModel461 => Path.Combine( SolutionFolder, "Tests", "BasicModels", "SqlActorPackage", "bin", Configuration, "net461" );
        public static string SqlActorPackageRuntime461 => Path.Combine( SolutionFolder, "Tests", "BasicModels", "SqlActorPackage.Runtime", "bin", Configuration, "net461" );


        #region Net standard component Paths
        // Currently, only Models are in netstandard2.0, Engines and runtimes are in NetCoreApp2.0
        // Engine and Runtimes may be in netstandard one day...
        //
        public static string StObjModelNet20 => Path.Combine( SolutionFolder, "CK.StObj.Model", "bin", Configuration, "netstandard2.0" );
        public static string StObjRuntimeNet20 => Path.Combine( SolutionFolder, "CK.StObj.Runtime", "bin", Configuration, "netcoreapp2.0" );
        public static string StObjEngineNet20 => Path.Combine( SolutionFolder, "CK.StObj.Engine", "bin", Configuration, "netcoreapp2.0" );

        public static string SetupableModelNet20 => Path.Combine( SolutionFolder, "CK.Setupable.Model", "bin", Configuration, "netstandard2.0" );
        public static string SetupableRuntimeNet20 => Path.Combine( SolutionFolder, "CK.Setupable.Runtime", "bin", Configuration, "netcoreapp2.0" );
        public static string SetupableEngineNet20 => Path.Combine( SolutionFolder, "CK.Setupable.Engine", "bin", Configuration, "netcoreapp2.0" );

        public static string SqlServerSetupModelNet20 => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Model", "bin", Configuration, "netstandard2.0" );
        public static string SqlServerSetupRuntimeNet20 => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Runtime", "bin", Configuration, "netcoreapp2.0" );
        public static string SqlServerSetupEngineNet20 => Path.Combine( SolutionFolder, "CK.SqlServer.Setup.Engine", "bin", Configuration, "netcoreapp2.0" );

        public static string SqlCallDemoNet20 => Path.Combine( SolutionFolder, "Tests", "SqlCallDemo", "SqlCallDemo", "bin", Configuration, "netstandard2.0" );

        public static string SqlCallDemoNetCoreTests20 => Path.Combine( SolutionFolder, "Tests", "SqlCallDemo", "SqlCallDemo.NetCore.Tests", "bin", Configuration, "netcoreapp2.0" );


        #endregion

        /// <summary>
        /// Gets a path to a zip.
        /// </summary>
        /// <param name="type">Type of the store.</param>
        /// <param name="suffix">Optional suffix (will appear before the .zip extension).</param>
        /// <param name="name">Name (automatically sets from the caller method name).</param>
        /// <returns>Path to a zip file that may already exists.</returns>
        public static string GetTestZipPath( TestStoreType type, string suffix = null, [CallerMemberName]string name = null )
        {
            return type == TestStoreType.Zip
                    ? Path.Combine( TestFolder, name + suffix + ".zip" )
                    : Path.Combine( TestFolder, "FolderStore", name + suffix );
        }

        /// <summary>
        /// Gets a free path to store (if the store exists it is deleted).
        /// </summary>
        /// <param name="type">Type of the store.</param>
        /// <param name="suffix">Optional suffix (will appear before the .zip extension).</param>
        /// <param name="name">Name (automatically sets from the caller method name).</param>
        /// <returns>Path to a zip file that does not exist.</returns>
        public static string GetCleanTestZipPath( TestStoreType type, string suffix = null, [CallerMemberName]string name = null )
        {
            var p = GetTestZipPath( type, suffix, name );
            if( type == TestStoreType.Zip ) File.Delete( p );
            else if( Directory.Exists( p ) ) Directory.Delete( p, true );
            return p;
        }


        static bool _net461Pushed;
        static bool _netStandardPushed;
        static Uri _storeUrl;

        public static Uri EnsureLocalCKDatabaseZipIsPushed( bool withNetStandard )
        {
            if( withNetStandard && _netStandardPushed ) return _storeUrl;
            if( !withNetStandard && _net461Pushed ) return _storeUrl;

            _storeUrl = TestHelper.EnsureCKSetupRemoteRunning();
            using( var source = TestHelper.OpenCKDatabaseZip( TestStoreType.Directory, withNetStandard ) )
            {
                source.PushComponents( c => true, _storeUrl, "HappyKey" ).Should().BeTrue();
            }
            if( withNetStandard ) _netStandardPushed = true;
            else _net461Pushed = true;
            return _storeUrl;
        }

        static bool[] _standardDbHasNet461 = new bool[Enum.GetNames( typeof( TestStoreType ) ).Length];
        static bool[] _standardDbHasNetStandard = new bool[Enum.GetNames( typeof( TestStoreType ) ).Length];

        public static string GetCKDatabaseZipPath(TestStoreType type) => GetTestZipPath( type, null, "Standard" );

        public static RuntimeArchive OpenCKDatabaseZip( TestStoreType type, bool withNetStandard = false )
        {
            string zipPath = GetCKDatabaseZipPath( type );
            RuntimeArchive zip = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, zipPath );
            if( !_standardDbHasNet461[(int)type] )
            {
                zip.CreateLocalImporter().AddComponent(
                    CKSetup.BinFolder.ReadBinFolder( Monitor, StObjRunnerNet461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, StObjModel461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, StObjRuntime461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, StObjEngine461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, SetupableModel461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, SetupableRuntime461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, SetupableEngine461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, SqlServerSetupModel461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, SqlServerSetupRuntime461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, SqlServerSetupEngine461 ),
                    CKSetup.BinFolder.ReadBinFolder( Monitor, CKSetupAppNet461 ) )
                .Import()
                .Should().BeTrue();
                _standardDbHasNet461[(int)type] = true;
            }
            #region NetStandard
            //// Net standard
            if( withNetStandard && !_standardDbHasNetStandard[(int)type] )
            {
                zip.CreateLocalImporter().AddComponent(
                        CKSetup.BinFolder.ReadBinFolder( Monitor, EnsurePublishPath( StObjRunnerNetCoreApp20 ) ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, StObjModelNet20 ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, EnsurePublishPath( StObjRuntimeNet20 ) ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, EnsurePublishPath( StObjEngineNet20 ) ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, SetupableModelNet20 ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, EnsurePublishPath( SetupableRuntimeNet20 ) ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, EnsurePublishPath( SetupableEngineNet20 ) ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, SqlServerSetupModelNet20 ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, EnsurePublishPath( SqlServerSetupRuntimeNet20 ) ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, EnsurePublishPath( SqlServerSetupEngineNet20 ) ),
                        CKSetup.BinFolder.ReadBinFolder( Monitor, EnsurePublishPath( CKSetupAppNetCoreApp20 ) ) )
                    .Import()
                    .Should().BeTrue();
                _standardDbHasNetStandard[(int)type] = true;
            }

            #endregion
            return zip;
        }

        public static void DeleteNet20PublishFolder()
        {
            using( Monitor.OpenInfo( "Deleting published Setup dependencies" ) )
            {
                DeleteDirectory( Path.Combine( StObjRunnerNetCoreApp20, "publish" ) );
                DeleteDirectory( Path.Combine( StObjRuntimeNet20, "publish" ) );
                DeleteDirectory( Path.Combine( StObjEngineNet20, "publish" ) );
                DeleteDirectory( Path.Combine( SetupableRuntimeNet20, "publish" ) );
                DeleteDirectory( Path.Combine( SetupableEngineNet20, "publish" ) );
                DeleteDirectory( Path.Combine( SqlServerSetupRuntimeNet20, "publish" ) );
                DeleteDirectory( Path.Combine( SqlServerSetupEngineNet20, "publish" ) );
                DeleteDirectory( Path.Combine( CKSetupAppNetCoreApp20, "publish" ) );
            }
            using( Monitor.OpenInfo( "Deleting published tests folders." ) )
            {
                DeleteDirectory( Path.Combine( SqlCallDemoNetCoreTests20, "publish" ) );
            }
        }

        public static void DeleteDirectory( string path )
        {
            using( Monitor.OpenInfo( $"Deleting '{path}'." ) )
            {
                int tryCount = 0;
                for(; ; )
                {
                    try
                    {
                        if( Directory.Exists( path ) ) Directory.Delete( path, true );
                        return;
                    }
                    catch( Exception ex )
                    {
                        if( ++tryCount == 20 ) throw;
                        Monitor.Info( "While cleaning up test directory. Retrying.", ex );
                        Thread.Sleep( 100 );
                    }
                }
            }
        }

        public static string EnsurePublishPath( string pathToFramework )
        {
            var publishPath = Path.Combine( pathToFramework, "publish" );
            if( !Directory.Exists( publishPath ) )
            {
                var framework = Path.GetFileName( pathToFramework );
                var pathToConfiguration = Path.GetDirectoryName( pathToFramework );
                var configuration = Path.GetFileName( pathToConfiguration );
                var projectPath = Path.GetDirectoryName( Path.GetDirectoryName( pathToConfiguration ) );
                var projectName = Path.GetFileName( projectPath );
                var pI = new ProcessStartInfo()
                {
                    WorkingDirectory = projectPath,
                    FileName = "dotnet",
                    Arguments = $"publish -c {configuration} -f {framework}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using( Monitor.OpenInfo( $"Publishing {projectName}: dotnet {pI.Arguments}" ) )
                using( Process cmdProcess = new Process() )
                {
                    cmdProcess.StartInfo = pI;
                    cmdProcess.ErrorDataReceived += ( o, e ) => { if( !string.IsNullOrEmpty( e.Data ) ) Monitor.Error( e.Data ); };
                    cmdProcess.OutputDataReceived += ( o, e ) => { if( e.Data != null ) Monitor.Info( e.Data ); };
                    cmdProcess.Start();
                    cmdProcess.BeginErrorReadLine();
                    cmdProcess.BeginOutputReadLine();
                    cmdProcess.WaitForExit();
                    if( cmdProcess.ExitCode != 0 )
                    {
                        Monitor.Error( $"Process returned ExitCode {cmdProcess.ExitCode}." );
                        return null;
                    }
                }
            }
            return publishPath;
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
            _testOutputPath = Path.Combine( SolutionFolder, "Tests", "CKSetup.Core.Tests", "TestOutput" );
            if( Directory.Exists( _testOutputPath ) )
            {
                Directory.Delete( _testOutputPath, true );
                // CreateDirectory is sometimes ignored.
                Thread.Sleep( 100 );
            }
            Directory.CreateDirectory( _testOutputPath );
            Console.WriteLine( $"SolutionFolder is: {_solutionFolder}." );
            Console.WriteLine( $"Core path: {typeof( string ).GetTypeInfo().Assembly.CodeBase}." );
        }

    }
}
