using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using System.IO;
using CKSetup.StreamStore;
using System.Reflection;

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
                CKSetup.SetupCommand.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlCallDemoModel461Path,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true
                    ).Should().Be( 0 );
            }
        }


        [Test]
        public void setup_SqlCallDemo_with_remote_imports()
        {
            string zipPath = TestHelper.GetCleanTestZipPath();
            using( var zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            using( var remoteZip = TestHelper.OpenCKDatabaseZip() )
            {
                CKSetup.SetupCommand.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlCallDemoModel461Path,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true,
                    missingImporter: new FakeRemote( remoteZip )
                    ).Should().Be( 0 );
            }
        }


        [Test]
        public void setup_SqlCallDemo_for_netstandard13()
        {
            Assume.That( false, "Support for netstandard/netcore has yet to be implemented." );
            using( var zip = TestHelper.OpenCKDatabaseZip( withNetStandard: true ) )
            {
                CKSetup.SetupCommand.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlCallDemoModelNet13Path,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true
                    ).Should().Be( 0 );
            }
        }

        [Test]
        public void setup_SqlActorPackage()
        {
            using( var zip = TestHelper.OpenCKDatabaseZip() )
            {
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageModel461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageRuntime461Path ) ).Should().BeTrue();
                CKSetup.SetupCommand.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlActorPackageModel461Path,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlActorPackage" ),
                    "SqlActorPackage.Generated.ByCKSetup",
                    sourceGeneration: true
                    ).Should().Be( 0 );
            }
        }
   }
}
