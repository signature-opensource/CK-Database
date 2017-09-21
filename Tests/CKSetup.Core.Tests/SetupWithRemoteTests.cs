using FluentAssertions;
using NUnit.Framework;
using System;

namespace CKSetup.Tests
{
    [TestFixture]
    public class SetupWithRemoteTests
    {
        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void push_to_the_remote_and_setup_SqlCallDemo461_from_scratch( TestStoreType sourceType )
        {
            Uri url = TestHelper.EnsureCKSetupRemoteRunning();
            using( var source = TestHelper.OpenCKDatabaseZip( sourceType ) )
            {
                source.PushComponents( c => true, url, "HappyKey" ).Should().BeTrue();
            }
            // Now use it...
            string archivePath = TestHelper.GetCleanTestZipPath( sourceType );
            using( var archive = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, archivePath ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.SqlCallDemo461,
                    archive,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true,
                    remoteStoreUrl: url
                    ).Should().BeTrue();
            }
        }

        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void push_to_the_remote_and_setup_SqlCallDemoNetCoreTests20_from_scratch( TestStoreType sourceType )
        {
            Uri url = TestHelper.EnsureCKSetupRemoteRunning();
            using( var source = TestHelper.OpenCKDatabaseZip( sourceType, withNetStandard: true ) )
            {
                source.PushComponents( c => true, url, "HappyKey" ).Should().BeTrue();
            }
            // Now use it...
            string archivePath = TestHelper.GetCleanTestZipPath( sourceType );
            using( var archive = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, archivePath ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.SqlCallDemoNetCoreTests20,
                    archive,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true,
                    remoteStoreUrl: url
                    ).Should().BeTrue();
            }
        }
    }
}
