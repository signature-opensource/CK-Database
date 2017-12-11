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
    /// Provides default implementation of <see cref="IMonitorTestHelper"/>.
    /// </summary>
    public class MonitorTestHelper : IMonitorTestHelper
    {
        IActivityMonitor _monitor;
        ActivityMonitorConsoleClient _console;
        readonly ITestHelperConfiguration _config;
        readonly IBasicTestHelper _basic;

        public MonitorTestHelper( ITestHelperConfiguration config, IBasicTestHelper basic )
        {
            _config = config;
            _basic = basic;
            LogFile.RootLogPath = basic.LogFolder;
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

        public string BuildConfiguration => _basic.BuildConfiguration;

        public NormalizedPath RepositoryFolder => _basic.RepositoryFolder;

        public NormalizedPath SolutionFolder => _basic.SolutionFolder;

        public NormalizedPath LogFolder => _basic.LogFolder;

        public NormalizedPath TestProjectFolder => _basic.TestProjectFolder;

        public NormalizedPath BinFolder => _basic.BinFolder;

        /// <summary>
        /// Gets the <see cref="IBasicTestHelper"/> default implementation.
        /// </summary>
        public static IMonitorTestHelper TestHelper { get; } = TestHelperResolver.Default.Resolve<IMonitorTestHelper>();

    }
}
