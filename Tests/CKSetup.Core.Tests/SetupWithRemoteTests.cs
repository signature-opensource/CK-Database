using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace CKSetup.Tests
{
    [TestFixture]
    public class SetupWithRemoteTests
    {

        [Test]
        public void push_to_the_remote_and_setup_SqlCallDemo461_from_scratch()
        {
            Uri storeUrl = TestHelper.EnsureLocalCKDatabaseZipIsPushed( withNetStandard: false );
            // Now use it...
            string archivePath = TestHelper.GetCleanTestZipPath( TestStoreType.Directory );
            using( var archive = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, archivePath ) )
            {
                Facade.DoSetup(
                    TestHelper.Monitor,
                    TestHelper.SqlCallDemo461,
                    archive,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    runnerLogFilter: LogFilter.Debug,
                    remoteStoreUrl: storeUrl
                    ).Should().BeTrue();
            }
        }

        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void push_to_the_remote_and_setup_SqlCallDemoNetCoreTests20_from_scratch( TestStoreType sourceType )
        {
            Uri storeUrl = TestHelper.EnsureLocalCKDatabaseZipIsPushed( withNetStandard: true );
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
                    runnerLogFilter: LogFilter.Debug,
                    remoteStoreUrl: storeUrl
                    ).Should().BeTrue();
            }
        }
    }
}
