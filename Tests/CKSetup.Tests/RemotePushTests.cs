using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CKSetup.StreamStore;
using System.IO;
using FluentAssertions;

namespace CKSetup.Tests
{
    [TestFixture]
    public class RemotePushTests
    {
        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void push_to_the_remote_and_setup_sqlCallDemo_from_scratch( TestStoreType sourceType )
        {
            Uri url = TestHelper.EnsureCKSetupRemoteRunning();
            using( var source = TestHelper.OpenCKDatabaseZip( sourceType ) )
            {
                source.PushComponents( c => true, url, "HappyKey" ).Should().BeTrue();
            }
            // Now use it...
            string zipPath = TestHelper.GetCleanTestZipPath( sourceType );
            using( var zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            {
                CKSetup.SetupCommand.DoSetup(
                    TestHelper.ConsoleMonitor,
                    TestHelper.SqlCallDemoModel461Path,
                    zip,
                    TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" ),
                    "SqlCallDemo.Generated.ByCKSetup",
                    sourceGeneration: true,
                    remoteStoreUrl: url
                    ).Should().Be( 0 );
            }

        }
    }
}
