using System;
using System.Reflection;
using CK.Core;
using CK.Setup;
using NUnit.Framework;
using CK.StObj.Engine.Tests.Poco;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class ConsoleToggle
    {
        [Test]
        public void toggle_console()
        {
            TestHelper.LogsToConsole = !TestHelper.LogsToConsole;
        }

    }
}
