using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.Setupable.Engine.Tests
{
    static class TestHelper
    {
        static string _solutionFolder;
        static string _configuration;
        static string _binFolder;
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
            get { if (_binFolder == null) InitalizePaths(); return _binFolder; }
        }

        public static string SolutionFolder
        {
            get { if (_solutionFolder == null) InitalizePaths(); return _solutionFolder; }
        }

        private static void InitalizePaths()
        {
#if NET461
            string p = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            p = Path.GetDirectoryName(p);
#else
            string p = Directory.GetCurrentDirectory();
#endif
#if DEBUG
            _configuration = "Debug";
#else
            _configuration = "Release";
#endif
            while (!Directory.EnumerateFiles(p).Where(f => f.EndsWith(".sln")).Any())
            {
                p = Path.GetDirectoryName(p);
            }
            _solutionFolder = p;
            _binFolder = Path.Combine(_solutionFolder, "Tests", "CK.Setupable.Engine.Tests", "bin", _configuration, "net451", "win7-x64");
            Console.WriteLine($"SolutionFolder is: {_solutionFolder}.");
            Console.WriteLine($"Core path: {typeof(string).GetType().Assembly.CodeBase}.");
        }

    }
}
