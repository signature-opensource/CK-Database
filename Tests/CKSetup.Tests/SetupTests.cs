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
        [TestCase( true )]
        [TestCase( false )]
        public void setup_SqlCallDemo( bool sourceGeneration )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip() )
            {
                CKSetup.SetupCommand.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlCallDemoModel461Path,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration
                    );
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
