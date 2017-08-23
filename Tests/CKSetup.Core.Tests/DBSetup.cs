using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using System.Diagnostics;
using CKSetup.Tests;

namespace CK.DB.Tests
{
    [TestFixture]
    public class DBSetup
    {
        [Test]
        [Explicit]
        public void toggle_logging_to_console()
        {
            TestHelper.LogToConsole = !TestHelper.LogToConsole;
        }

    }
}
