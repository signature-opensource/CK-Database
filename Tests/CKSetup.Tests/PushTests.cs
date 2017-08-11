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
    public class PushTests
    {
        [TestCase( TestStoreType.Zip, TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory, TestStoreType.Directory )]
        [TestCase( TestStoreType.Zip, TestStoreType.Directory )]
        [TestCase( TestStoreType.Directory, TestStoreType.Zip )]
        public void push_to_a_fake_remote( TestStoreType sourceType, TestStoreType targetType )
        {
            string zipPath = TestHelper.GetCleanTestZipPath( targetType, "-From-"+ sourceType );
            using( var target = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            using( var source = TestHelper.OpenCKDatabaseZip( sourceType ) )
            {
                source.PushComponents( c => true, new FakeRemote( target ) ).Should().BeTrue();
            }
            var sourceContent = new StoreContent( TestHelper.GetCKDatabaseZipPath( sourceType ) );
            var targetContent = new StoreContent( zipPath );
            sourceContent.ShouldBeEquivalentTo( targetContent );
        }
    }
}
