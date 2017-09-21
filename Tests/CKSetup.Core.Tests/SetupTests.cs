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
                    TestHelper.Monitor,
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
        public void setup_SqlCallDemoNet20_fails( TestStoreType type )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type, withNetStandard: true ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.SqlCallDemoNet20,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true
                    ).Should().BeFalse();
            }
        }

        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void setup_SqlCallDemoNet20_publish_folder_fails( TestStoreType type )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type, withNetStandard: true ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.EnsurePublishPath( TestHelper.SqlCallDemoNet20 ),
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true
                    ).Should().BeFalse();
            }
        }

        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void setup_SqlCallDemoNetCoreTests20( TestStoreType type )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type, withNetStandard: true ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.SqlCallDemoNetCoreTests20,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true
                    ).Should().BeTrue();
            }
        }

        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void setup_SqlCallDemo461_with_remote_imports(TestStoreType type)
        {
            string zipPath = TestHelper.GetCleanTestZipPath( type );
            using( var zip = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, zipPath ) )
            using( var remoteZip = TestHelper.OpenCKDatabaseZip( type ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
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
        public void setup_SqlActorPackageModel461( TestStoreType type )
        {
            string zipPath = TestHelper.GetCleanTestZipPath( type );
            using( var zip = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, zipPath ) )
            using( var remoteZip = TestHelper.OpenCKDatabaseZip( type ) )
            {
                var missingImporter = new FakeRemote( remoteZip );
                zip.CreateLocalImporter( missingImporter ).AddComponent( 
                    BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.SqlActorPackageModel461 ),
                    BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.SqlActorPackageRuntime461 ) )
                    .Import()
                    .Should().BeTrue();
                Facade.DoSetup(
                    TestHelper.Monitor,
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
        public void setup_SqlActorPackageModel461_without_its_runtime_fails( TestStoreType type )
        {
            string zipPath = TestHelper.GetCleanTestZipPath( type );
            using( var zip = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, zipPath ) )
            using( var remoteZip = TestHelper.OpenCKDatabaseZip( type ) )
            {
                var missingImporter = new FakeRemote( remoteZip );
                zip.CreateLocalImporter( missingImporter ).AddComponent(
                    BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.SqlActorPackageModel461 ) )
                    .Import()
                    .Should().BeTrue();
                Facade.DoSetup(
                    TestHelper.Monitor,
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
