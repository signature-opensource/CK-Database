using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CK.Core;
using CK.SqlServer.Setup;

namespace CK.Deploy.Console
{
    class Program
    {
        public static void PrintUsage( TextWriter output )
        {
            output.WriteLine( "V 1" );
            output.WriteLine( "CK.Deploy.Console.exe <path> <connectionString>" );
            output.WriteLine();
            output.WriteLine( "<path>\t root directory to process" );
            output.WriteLine( "<connectionString>\t Connection String" );
            output.WriteLine();
            output.WriteLine( "V 2" );
            output.WriteLine( "CK.Deploy.Console.exe -v2 <rootAbsolutePath> <path1>;<path2>;<pathX> <path1>;<path2>;<pathX> <assName1>;<assName2>;<assNameX> <connectionString>" );
            output.WriteLine();
            output.WriteLine( "<rootAbsolutePath>\t\t Root absolute directory of the project" );
            output.WriteLine( "<filePathX>\t\t relative path of directories to process as file based process" );
            output.WriteLine( "<dllPathX>\t\t relative path of directories to process as StObj based process" );
            output.WriteLine( "<assNameX>\t\t assemblyName to process" );
            output.WriteLine( "<connectionString>\t\t Connection String" );
        }

        static Semaphore _semaphore;

        static void Main(string[] args)
        {
            if( args.Contains( "/?" ) )
            {
                PrintUsage( System.Console.Out );
                return;
            }

            ArgsAnalyzer analyzer = new ArgsAnalyzer( args );
            string result = analyzer.Analyze();
            if( analyzer.IsValid )
            {
                if( analyzer.IsV2 )
                {
                    AppDomain app = PrepareAppDomain( analyzer.V2Args );
                    using( _semaphore = new Semaphore( 0, 1 ) )
                    {
                        app.SetData( "MainArgs", analyzer.V2Args );
                        app.DoCallBack( new CrossAppDomainDelegate( RunV2 ) );
                        _semaphore.WaitOne();
                    }
                }
                else
                {
                    RunV1( analyzer.V1Args );
                }
            }
            else
            {
                System.Console.Out.Write( result );
                PrintUsage( System.Console.Out );
            }
        }

        private static AppDomain PrepareAppDomain( V2Args args )
        {
            // Find real ApplicationBase
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            // Code base is like "file:///C:/Users/Spi/Documents/Dev4/CK-Database/Output/Tests/Debug/CK.Setup.SqlServer.Tests.DLL"
            if( !codeBase.StartsWith( "file:///" ) )
                throw new ApplicationException( "Code base must start with file:/// protocol." );
            codeBase = codeBase.Substring( 8 ).Replace( '/', System.IO.Path.DirectorySeparatorChar );


            DirectoryInfo codeBaseDir = new DirectoryInfo( codeBase ).Parent; // Strip file name
            DirectoryInfo projectRootDir = new DirectoryInfo( args.AbsoluteRootPath );

            Ancestor.FinderResult result = Ancestor.FindCommonAncestor( codeBaseDir, projectRootDir );
            if( result == null )
                throw new ApplicationException( string.Format( "Code base must has common ancestor with AbsoluteRootPath. No ancestor can be found from {0} and {1}", codeBaseDir.FullName, projectRootDir.FullName) );

            var dllProb = args.RelativeDllPaths.Select( x => Path.Combine( result.ProjectRootRelativePath, x ) ).ToList();
            dllProb.Add( result.CodeBaseRelativePath ); // inject path for this program in the new appdomain

            System.Console.WriteLine( "Creating AppDomain with :" );
            System.Console.WriteLine( "\tApplicationBase : {0}", result.CommonPath );
            System.Console.WriteLine( "\tPrivateBinPath : {0}", string.Join( ";", dllProb ) );

            AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
            setup.ApplicationBase = result.CommonPath;
            //setup.DisallowApplicationBaseProbing = true; 
            setup.PrivateBinPathProbe = "*";
            setup.PrivateBinPath = string.Join( ";", dllProb );
            return AppDomain.CreateDomain( "MyApp", null, setup );
        }

        public static void RunV1( V1Args args )
        {
            var console = new ActivityLoggerConsoleSink();
            var logger = DefaultActivityLogger.Create();
            logger.Tap.Register( console );

            using( logger.OpenGroup( LogLevel.Info, "Begin dbSetup with:" ) )
            {
                logger.Info( string.Format( "FilePath: {0}", args.FilePath ) );
                logger.Info( "ConnectionString: " + args.ConnectionString );
            }
            
            var config = new SqlSetupCenterConfiguration();
            config.DefaultDatabaseConnectionString = args.ConnectionString;
            config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;
            config.FilePackageDirectories.Add( args.FilePath );
            config.SqlFileDirectories.Add( args.FilePath );

            using( SqlSetupCenter c = new SqlSetupCenter( logger, config ) )
            {
                c.Run();
            }
        }

        public static void RunV2()
        {
            try
            {
                V2Args args = (V2Args)AppDomain.CurrentDomain.GetData( "MainArgs" );

                var console = new ActivityLoggerConsoleSink();
                var logger = DefaultActivityLogger.Create();
                logger.Tap.Register( console );

                using( logger.OpenGroup( LogLevel.Info, "Begin dbSetup with:" ) )
                {
                    logger.Info( string.Format( "RootPath: {0}", args.AbsoluteRootPath ) );
                    logger.Info( string.Format( "FilePaths: {0}", string.Join( ", ", args.RelativeFilePaths ) ) );
                    logger.Info( string.Format( "DllPaths: {0}", string.Join( ", ", args.RelativeDllPaths ) ) );
                    logger.Info( string.Format( "Assembly: {0}", string.Join( ", ", args.AssemblyNames ) ) );
                    logger.Info( "ConnectionString: " + args.ConnectionString );
                }

                var config = new SqlSetupCenterConfiguration();
                config.DefaultDatabaseConnectionString = args.ConnectionString;
                config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;
                var rootedPaths = args.RelativeFilePaths.Select( p => Path.Combine( args.AbsoluteRootPath, p ) );
                config.FilePackageDirectories.AddRange( rootedPaths );
                config.SqlFileDirectories.AddRange( rootedPaths );
                config.SetupConfiguration.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.AddRange( args.AssemblyNames );
                using( SqlSetupCenter c = new SqlSetupCenter( logger, config ) )
                {
                    c.Run();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
