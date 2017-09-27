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
        [TestCase( TestStoreType.Zip, "Monitor" )]
        [TestCase( TestStoreType.Directory, "Trace" )]
        public void setup_SqlCallDemo461( TestStoreType type, string logFilter )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.SqlCallDemo461,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true,
                    runnerLogFilter: LogFilter.Parse( logFilter )
                    ).Should().BeTrue();
            }
        }

        [TestCase( TestStoreType.Zip, "Release" )]
        [TestCase( TestStoreType.Directory, "Verbose" )]
        public void setup_SqlCallDemoNet20_fails( TestStoreType type, string logFilter )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type, withNetStandard: true ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.SqlCallDemoNet20,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true,
                    runnerLogFilter: LogFilter.Parse( logFilter )
                    ).Should().BeFalse();
            }
        }

        [TestCase( TestStoreType.Zip, "Off" )]
        [TestCase( TestStoreType.Directory, "Debug" )]
        public void setup_SqlCallDemoNet20_publish_folder_fails( TestStoreType type, string logFilter )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type, withNetStandard: true ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.EnsurePublishPath( TestHelper.SqlCallDemoNet20 ),
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true,
                    runnerLogFilter: LogFilter.Parse( logFilter )
                    ).Should().BeFalse();
            }
        }

        [TestCase( TestStoreType.Zip, "Terse" )]
        [TestCase( TestStoreType.Directory, "Undefined" )]
        public void setup_SqlCallDemoNetCoreTests20( TestStoreType type, string logFilter )
        {
            using( var zip = TestHelper.OpenCKDatabaseZip( type, withNetStandard: true ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.SqlCallDemoNetCoreTests20,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true,
                    runnerLogFilter: LogFilter.Parse( logFilter )
                    ).Should().BeTrue();
            }
        }

        [TestCase( TestStoreType.Zip, "Terse" )]
        [TestCase( TestStoreType.Directory, "Trace" )]
        public void setup_SqlCallDemo461_with_remote_imports(TestStoreType type,string logFilter)
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
                    runnerLogFilter: LogFilter.Parse( logFilter ),
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
                    runnerLogFilter: LogFilter.Terse,
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
                    runnerLogFilter: LogFilter.Verbose,
                    missingImporter: missingImporter
                    ).Should().BeFalse();
            }
        }

    }
}
