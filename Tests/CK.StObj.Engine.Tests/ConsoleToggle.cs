using System;
using System.Reflection;
using CK.Core;
using CK.Setup;
using NUnit.Framework;
using CK.StObj.Engine.Tests.Poco;
using System.Diagnostics;

using static CK.Testing.MonitorTestHelper;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class ConsoleToggle
    {
        [Explicit]
        [Test]
        public void toggle_console()
        {
            TestHelper.LogToConsole = !TestHelper.LogToConsole;
            TestHelper.Monitor.MinimalFilter = LogFilter.Debug;
        }

        [Explicit]
        [Test]
        public void launch_debugger()
        {
            if (!Debugger.IsAttached) Debugger.Launch();
        }

    }
}
