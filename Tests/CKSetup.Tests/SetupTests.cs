using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.Tests
{
    [TestFixture]
    public class SetupTests
    {
        [Test]
        public void setup_SqlCallDemo()
        {
            using( var zip = TestHelper.OpenCKDatabaseZip() )
            {

            }
        }

        [Test]
        public void setup_SqlActorPackage()
        {
            using( var zip = TestHelper.OpenCKDatabaseZip() )
            {
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageModel461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageRuntime461Path ) ).Should().BeTrue();
            }
        }
   }
}
