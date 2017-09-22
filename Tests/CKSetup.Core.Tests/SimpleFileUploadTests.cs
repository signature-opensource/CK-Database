using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.Tests
{
    [TestFixture]
    public class SimpleFileUploadTests
    {
        static string GetThisFilePath( [CallerFilePath]string p = null ) => p;

        [Test]
        public void upload_file_to_remote_store_and_get_it_back()
        {
            Uri storeUrl = TestHelper.EnsureCKSetupRemoteRunning();
            HttpClient client = new HttpClient();
            var urlUp = storeUrl + ClientRemoteStore.RootPathString.Substring( 1 ) + "/upload";
            using( var c = new StreamContent( File.OpenRead( GetThisFilePath() ) ) )
            {
                c.Headers.Add( ClientRemoteStore.ApiKeyHeader, "HappyKey" );
                c.Headers.Add( "FileName", "Setup.zip" );
                c.Headers.Add( "Version", "1.0.0-alpha" );
                c.Headers.Add( "AllowOverwrite", "true" );
                using( HttpResponseMessage r = client.PostAsync( urlUp, c ).GetAwaiter().GetResult() )
                {
                    r.IsSuccessStatusCode.Should().BeTrue();
                }
            }
            var stored = client.GetStringAsync( storeUrl + "Files/Setup.zip/1.0.0-alpha.zip" ).GetAwaiter().GetResult();
            stored.Should().Be( File.ReadAllText( GetThisFilePath() ) );
        }


    }
}
