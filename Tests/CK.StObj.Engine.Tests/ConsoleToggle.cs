using System;
using System.Reflection;
using CK.Core;
using CK.Setup;
using NUnit.Framework;
using CK.StObj.Engine.Tests.Poco;
using System.Diagnostics;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class ConsoleToggle
    {
        [Explicit]
        [Test]
        public void toggle_console()
        {
            TestHelper.LogsToConsole = !TestHelper.LogsToConsole;
        }

        [Explicit]
        [Test]
        public void launch_debugger()
        {
            if (!Debugger.IsAttached) Debugger.Launch();
        }

    }
}
