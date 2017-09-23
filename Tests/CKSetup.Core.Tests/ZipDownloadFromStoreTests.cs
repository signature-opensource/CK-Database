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

        [TestCase( "Net461" )]
        [TestCase( "NetCoreApp20" )]
        public async Task downloading_cksetup_itself_from_remote_store( string runtime )
        {
            string folderForCKSetup = TestHelper.GetCleanTestZipPath( TestStoreType.Directory );
            Uri storeUrl = TestHelper.EnsureLocalCKDatabaseZipIsPushed( runtime == "NetCoreApp20" );
            using( var body = await TestHelper.SharedHttpClient.GetStreamAsync( storeUrl + "dl-zip/CKSetup/" + runtime ) )
            using( var mem = new MemoryStream() )
            {
                await body.CopyToAsync( mem );
                mem.Position = 0;
                using( var z = new ZipArchive( mem, ZipArchiveMode.Read ) )
                {
                    z.ExtractToDirectory( folderForCKSetup );
                }

                var original = runtime == "NetCoreApp20"
                                ? Path.Combine( TestHelper.CKSetupAppNetCoreApp20, "publish" )
                                : TestHelper.CKSetupAppNet461;

                Directory.EnumerateFiles( original, "*.dll", SearchOption.AllDirectories )
                         .Select( s => s.Substring( original.Length ) )
                         .OrderBy( s => s )
                    .SequenceEqual( Directory.EnumerateFiles( folderForCKSetup, "*.dll", SearchOption.AllDirectories )
                                                    .Select( s => s.Substring( folderForCKSetup.Length ) )
                                                    .OrderBy( s => s ) )
                    .Should().BeTrue();
            }
        }



     }
}
