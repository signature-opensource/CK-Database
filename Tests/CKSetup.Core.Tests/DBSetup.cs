using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using System.Diagnostics;
using CKSetup.Tests;

namespace CK.DB
{
    [TestFixture]
    public class TestController
    {
        [Test]
        [Explicit]
        public void toggle_logging_to_console()
        {
            TestHelper.LogToConsole = !TestHelper.LogToConsole;
        }

        [Test]
        [Explicit]
        public void delete_Net20_publish_folder()
        {
            TestHelper.DeleteNet20PublishFolder();
        }

    }
}
