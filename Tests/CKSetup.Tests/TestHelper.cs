using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CK.Core;
using NUnit.Framework;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        static TestHelper()
        {
            _monitor = new ActivityMonitor();
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

        public static string SetupableRuntime461Path => Path.Combine( SolutionFolder, "CK.Setupable.Runtime", "bin", "Debug", "net461" );

        public static string SetupableModel461Path => Path.Combine( SolutionFolder, "CK.Setupable.Model", "bin", "Debug", "net461" );

        public static string GetTestZipPath( string suffix = null, [CallerMemberName]string name = null )
        {
            return Path.Combine( TestFolder, name + suffix + ".zip" );
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
