using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CK.Core;
using CK.Text;

namespace CK.Testing
{

    /// <summary>
    /// Provides default implementation of <see cref="IMonitorTestHelperCore"/>.
    /// </summary>
    public class MonitorTestHelper : IMonitorTestHelperCore
    {
        IActivityMonitor _monitor;
        ActivityMonitorConsoleClient _console;
        readonly ITestHelperConfiguration _config;

        public MonitorTestHelper( ITestHelperConfiguration config, IBasicTestHelper basic )
        {
            _config = config;
            LogFile.RootLogPath = basic.LogFolder;
            LogToConsole = _config.GetBoolean( "Monitor/LogToConsole" ) ?? false;
        }

        public IActivityMonitor Monitor
        {
            get
            {
                if( _monitor == null )
                {
                    _monitor = new ActivityMonitor();
                    _console = new ActivityMonitorConsoleClient();
                }
                return _monitor;
            }
        }

        public bool LogToConsole
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
        /// Gets the <see cref="IBasicTestHelper"/> default implementation.
        /// </summary>
        public static IMonitorTestHelper TestHelper { get; } = TestHelperResolver.Default.Resolve<IMonitorTestHelper>();

    }
}
