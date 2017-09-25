using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup.Tests
{
    [TestFixture]
    public class ZipDownloadFromStoreTests
    {

        [TestCase( "Net461" )]
        [TestCase( "NetCoreApp20" )]
        public async Task downloading_cksetup_itself_from_remote_store( string runtime )
        {
            string folderForCKSetup = TestHelper.GetCleanTestZipPath( TestStoreType.Directory, runtime );
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

        [TestCase( "Net461" )]
        [TestCase( "NetCoreApp20" )]
        public async Task getting_component_info( string runtime )
        {
            string folderForCKSetup = TestHelper.GetCleanTestZipPath( TestStoreType.Directory, runtime );
            Uri storeUrl = TestHelper.EnsureLocalCKDatabaseZipIsPushed( runtime == "NetCoreApp20" );
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.BadRequest );
            }
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info/Unk", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.BadRequest );
            }
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info/Unk/not-a-runtime/", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.BadRequest );
            }
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info/Unk/not-a-runtime/1.0.0", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.BadRequest );
            }
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info/Unk/Net461/not-a-version", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.BadRequest );
            }
            // Component is not found: returns 204 NoContent.
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info/Unk/NetCoreApp20/1.0.0-alpha", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.NoContent );
            }
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info/CKSetup/Net461/1.0.0-alpha", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.NoContent );
            }
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info/CKSetup/Net47", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.NoContent );
            }
            // Component found: returns Component xml representation.
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info/CKSetup/Net461", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.OK );
                var c = new Component( XElement.Parse( await msg.Content.ReadAsStringAsync() ) );
                c.ComponentKind.Should().Be( ComponentKind.SetupDependency );
                c.Files.Should().NotBeEmpty();
            }
            using( var msg = await TestHelper.SharedHttpClient.GetAsync( storeUrl + "component-info/CKSetup/Net461/", HttpCompletionOption.ResponseHeadersRead ) )
            {
                msg.StatusCode.Should().Be( HttpStatusCode.OK );
                var c = new Component( XElement.Parse( await msg.Content.ReadAsStringAsync() ) );
                c.ComponentKind.Should().Be( ComponentKind.SetupDependency );
                c.Files.Should().NotBeEmpty();
            }
        }




    }
}
