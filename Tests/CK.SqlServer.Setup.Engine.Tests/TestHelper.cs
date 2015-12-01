//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using CK.Core;
//using CK.Setup;
//using NUnit.Framework;

//namespace CK.SqlServer.Setup.Engine.Tests
//{
//    static class TestHelper
//    {
//        static string _testBinFolder;
//        static string _solutionFolder;
//        static string _scriptFolder;

//        static IActivityMonitor _monitor;
//        static ActivityMonitorConsoleClient _console;

//        static TestHelper()
//        {
//            _monitor = new ActivityMonitor();
//            _monitor.Output.BridgeTarget.HonorMonitorFilter = false;
//            _console = new ActivityMonitorConsoleClient();
//        }

//        public const string MasterConnection = "Server=(local)\\NIMP;Database=master;Integrated Security=SSPI";

//        public static IActivityMonitor ConsoleMonitor
//        {
//            get { return _monitor; }
//        }

//        public static bool LogsToConsole
//        {
//            get { return _monitor.Output.Clients.Contains( _console ); }
//            set
//            {
//                if( value ) _monitor.Output.RegisterUniqueClient( c => c == _console, () => _console );
//                else _monitor.Output.UnregisterClient( _console );
//            }
//        }

//        public static string TestBinFolder
//        {
//            get { if( _testBinFolder == null ) InitalizePaths(); return _testBinFolder; }
//        }

//        public static string SolutionDirectory
//        {
//            get { if( _solutionFolder == null ) InitalizePaths(); return _solutionFolder; }
//        }

//        public static string FolderScript
//        {
//            get { if( _scriptFolder == null ) InitalizePaths(); return _scriptFolder; }
//        }

//        public static string GetScriptsFolder( string testName )
//        {
//            return Path.Combine( FolderScript, testName );
//        }

//        private static void InitalizePaths()
//        {
//            string p = new Uri( System.Reflection.Assembly.GetExecutingAssembly().CodeBase ).LocalPath;
//            // => CK.XXX.Tests/bin/Debug/
//            p = Path.GetDirectoryName( p );
//            _testBinFolder = p;
//            do
//            {
//                p = Path.GetDirectoryName( p );
//            }
//            while( !File.Exists( Path.Combine( p, "CK-Database.sln" ) ) );
//            _solutionFolder = p;
//            // ==> Tests/CK.SqlServer.Setup.Engine.Tests/Scripts
//            _scriptFolder = Path.Combine( p, "Tests", "CK.SqlServer.Setup.Engine.Tests", "Scripts" );
//        }
//    }
//}
