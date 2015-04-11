#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Deploy.Console\Program.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CK.Core;
using CK.Monitoring;
using CK.Setup;
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

        static void Main( string[] args )
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
                    app.SetData( "MainArgs", analyzer.V2Args );
                    app.DoCallBack( new CrossAppDomainDelegate( RunV2 ) );
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
                throw new ApplicationException( string.Format( "Code base must has common ancestor with AbsoluteRootPath. No ancestor can be found from {0} and {1}", codeBaseDir.FullName, projectRootDir.FullName ) );

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
            var monitor = new ActivityMonitor();
            monitor.Output.RegisterClient( new ActivityMonitorConsoleClient() );

            using( monitor.OpenInfo().Send( "Begin dbSetup with:" ) )
            {
                monitor.Info().Send( string.Format( "FilePath: {0}", args.FilePath ) );
                monitor.Info().Send( "ConnectionString: " + args.ConnectionString );
            }

            var config = new SqlSetupAspectConfiguration();
            config.DefaultDatabaseConnectionString = args.ConnectionString;
            config.FilePackageDirectories.Add( args.FilePath );
            config.SqlFileDirectories.Add( args.FilePath );
            var c = new SetupEngineConfiguration();
            c.Aspects.Add( config );
            c.StObjEngineConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;

            StObjContextRoot.Build( c, null, monitor ).Dispose();
        }

        public static void RunV2()
        {
            V2Args args = (V2Args)AppDomain.CurrentDomain.GetData( "MainArgs" );

            SystemActivityMonitor.RootLogPath = args.LogPath;
            GrandOutput.EnsureActiveDefaultWithDefaultSettings();
            
            var monitor = new ActivityMonitor();
            monitor.Output.RegisterClient( new ActivityMonitorConsoleClient() );

            using( monitor.OpenInfo().Send( "Begin dbSetup with:" ) )
            {
                monitor.Info().Send( string.Format( "RootPath: {0}", args.AbsoluteRootPath ) );
                monitor.Info().Send( string.Format( "FilePaths: {0}", string.Join( ", ", args.RelativeFilePaths ) ) );
                monitor.Info().Send( string.Format( "DllPaths: {0}", string.Join( ", ", args.RelativeDllPaths ) ) );
                monitor.Info().Send( string.Format( "Assembly: {0}", string.Join( ", ", args.AssemblyNames ) ) );
                monitor.Info().Send( "ConnectionString: " + args.ConnectionString );
            }

            var config = new SqlSetupAspectConfiguration();
            config.DefaultDatabaseConnectionString = args.ConnectionString;
            var rootedPaths = args.RelativeFilePaths.Select( p => Path.Combine( args.AbsoluteRootPath, p ) );
            config.FilePackageDirectories.AddRange( rootedPaths );
            config.SqlFileDirectories.AddRange( rootedPaths );
            var c = new SetupEngineConfiguration();
            c.Aspects.Add( config );
            c.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.AddRange( args.AssemblyNames );
            c.StObjEngineConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = false;

            StObjContextRoot.Build( c, null, monitor ).Dispose();
        }
    }
}
