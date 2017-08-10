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
        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void setup_SqlCallDemo( TestStoreType type )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type ) )
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


        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void setup_SqlCallDemo_with_remote_imports(TestStoreType type)
        {
            string zipPath = TestHelper.GetCleanTestZipPath( type );
            using( var zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            using( var remoteZip = TestHelper.OpenCKDatabaseZip( type ) )
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


        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void setup_SqlCallDemo_for_netstandard13( TestStoreType type )
        {
            Assume.That( false, "Support for netstandard/netcore has yet to be implemented." );
            using( var zip = TestHelper.OpenCKDatabaseZip( type, withNetStandard: true ) )
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

        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void setup_SqlActorPackage( TestStoreType type )
        {
            string zipPath = TestHelper.GetCleanTestZipPath( type );
            using( var zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            using( var remoteZip = TestHelper.OpenCKDatabaseZip( type ) )
            {
                var missingImporter = new FakeRemote( remoteZip );
                zip.CreateLocalImporter( missingImporter ).AddComponent( 
                    BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageModel461Path ),
                    BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageRuntime461Path ) )
                    .Import()
                    .Should().BeTrue();
                CKSetup.SetupCommand.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlActorPackageModel461Path,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlActorPackage" ),
                    "SqlActorPackage.Generated.ByCKSetup",
                    sourceGeneration: true,
                    missingImporter: missingImporter
                    ).Should().Be( 0 );
            }
        }
   }
}
