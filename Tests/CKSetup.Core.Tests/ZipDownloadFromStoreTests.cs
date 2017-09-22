using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.Tests
{
    [TestFixture]
    public class ZipDownloadFromStoreTests
    {

        [Test]
        public async Task downloading_cksetup_itself_from_remote_store()
        {
            string folderForCKSetup = TestHelper.GetCleanTestZipPath( TestStoreType.Directory );
            Uri storeUrl = TestHelper.EnsureLocalCKDatabaseZipIsPushed( withNetStandard: false );
            using( var body = await TestHelper.SharedHttpClient.GetStreamAsync( storeUrl + "/dl-zip/CKSetup/Net461" ) )
            using( var mem = new MemoryStream() )
            {
                await body.CopyToAsync( mem );
                mem.Position = 0;
                using( var z = new ZipArchive( mem, ZipArchiveMode.Read ) )
                {
                    z.ExtractToDirectory( folderForCKSetup );
                }
                Directory.EnumerateFiles( TestHelper.CKSetupAppNet461, "*.dll", SearchOption.AllDirectories )
                    .ShouldBeEquivalentTo( Directory.EnumerateFiles( folderForCKSetup, "*.dll", SearchOption.AllDirectories ));
            }
        }

     }
}
