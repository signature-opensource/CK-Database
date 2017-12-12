using CK.Core;
using System;

namespace CK.Testing
{
    /// <summary>
    /// Provides a monitor and console control.
    /// </summary>
    public interface IMonitorTestHelperCore : ITestHelper
    {
        /// <summary>
        /// Gets the monitor.
        /// </summary>
        IActivityMonitor Monitor { get; }

        /// <summary>
        /// Gets or sets whether <see cref="Monitor"/> will log into the console.
        /// Initially configurable by "Monitor/LogToConsole" = "true", otherwise defaults to false.
        /// </summary>
        bool LogToConsole { get; set; }

    }
}
