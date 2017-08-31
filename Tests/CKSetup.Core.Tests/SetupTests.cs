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
        public void setup_SqlCallDemo461( TestStoreType type )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type ) )
            {
                Facade.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlCallDemo461,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true
                    ).Should().BeTrue();
            }
        }


        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void setup_SqlCallDemo_for_netstandard13( TestStoreType type )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type, withNetStandard: true ) )
            {
                Facade.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.EnsurePublishPath( TestHelper.SqlCallDemoNet13 ),
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true
                    ).Should().BeTrue();
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
                Facade.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlCallDemo461,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true,
                    missingImporter: new FakeRemote( remoteZip )
                    ).Should().BeTrue();
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
                    BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.EnsurePublishPath( TestHelper.SqlActorPackageModel461 ) ),
                    BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.EnsurePublishPath( TestHelper.SqlActorPackageRuntime461 ) ) )
                    .Import()
                    .Should().BeTrue();
                Facade.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlActorPackageModel461,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlActorPackage" ),
                    "SqlActorPackage.Generated.ByCKSetup",
                    sourceGeneration: true,
                    missingImporter: missingImporter
                    ).Should().BeTrue();
            }
        }

        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void setup_SqlActorPackage_without_its_runtime_fails( TestStoreType type )
        {
            string zipPath = TestHelper.GetCleanTestZipPath( type );
            using( var zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            using( var remoteZip = TestHelper.OpenCKDatabaseZip( type ) )
            {
                var missingImporter = new FakeRemote( remoteZip );
                zip.CreateLocalImporter( missingImporter ).AddComponent(
                    BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.EnsurePublishPath( TestHelper.SqlActorPackageModel461 ) ) )
                    .Import()
                    .Should().BeTrue();
                Facade.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlActorPackageModel461,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlActorPackage" ),
                    "SqlActorPackage.Generated.ByCKSetup",
                    sourceGeneration: true,
                    missingImporter: missingImporter
                    ).Should().BeFalse();
            }
        }

    }
}
