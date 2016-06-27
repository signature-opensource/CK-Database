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
    }
}
