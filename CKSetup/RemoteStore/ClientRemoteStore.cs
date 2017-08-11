using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CKSetup.StreamStore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO.Compression;

namespace CKSetup
{
    /// <summary>
    /// Wraps a <see cref="HttpClient"/>.
    /// Uses <see cref="Task.GetAwaiter"/> to perform sync to HttpClient async calls.
    /// </summary>
    /// <remarks>
    /// See https://stackoverflow.com/questions/43382460/multiple-calls-to-static-httpclient-hangs-console-application and 
    /// https://stackoverflow.com/questions/9343594/how-to-call-asynchronous-method-from-synchronous-method-in-c.
    /// </remarks>
    public class ClientRemoteStore : IComponentImporter, IComponentPushTarget, IDisposable
    {
        static public readonly string ApiKeyHeader = "X-API-Key";
        static public readonly string SessionIdHeader = "X-SessionId";
        static public readonly string RootPathString = "/.cksetup/store";
        static public readonly string PushPath = "/push";
        static public readonly string PushFilePath = "/fp";

        readonly HttpClient _client;
        readonly Uri _remoteStoreUrl;
        readonly string _pushApiKey;

        public ClientRemoteStore( Uri remoteStoreUrl, string pushApiKey )
        {
            _client = new HttpClient();
            _remoteStoreUrl = remoteStoreUrl;
            _pushApiKey = pushApiKey;
        }

        StoredStream IComponentFileDownloader.GetDownloadStream( IActivityMonitor monitor, SHA1Value file, CompressionKind kind )
        {
            throw new NotImplementedException();
        }

        Stream IComponentImporter.OpenImportStream( IActivityMonitor monitor, ComponentMissingDescription missing )
        {
            throw new NotImplementedException();
        }

        PushComponentsResult IComponentPushTarget.PushComponents( IActivityMonitor monitor, Action<Stream> componentsWriter )
        {
            try
            {
                using( var buffer = new MemoryStream() )
                {
                    componentsWriter( buffer );
                    buffer.Position = 0;
                    using( var c = new StreamContent( buffer ) )
                    {
                        c.Headers.Add( ApiKeyHeader, _pushApiKey );
                        using( HttpResponseMessage r = _client.PostAsync( _remoteStoreUrl + PushPath, c ).GetAwaiter().GetResult() )
                        using( var responseStream = r.Content.ReadAsStreamAsync().GetAwaiter().GetResult() )
                        {
                            return new PushComponentsResult( new CKBinaryReader( responseStream ) );
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                monitor.Error( "Client call error.", ex );
                return new PushComponentsResult( ex.Message, null );
            }
        }

        bool IComponentPushTarget.PushFile( IActivityMonitor monitor, string sessionId, SHA1Value sha1, Action<Stream> writer, CompressionKind kind )
        {
            try
            {
                using( var buffer = new MemoryStream() )
                {
                    writer = StreamStoreExtension.GetCompressShell( kind, writer );
                    writer( buffer );
                    buffer.Position = 0;
                    using( var c = new StreamContent( buffer ) )
                    {
                        c.Headers.Add( SessionIdHeader, sessionId );
                        var url = _remoteStoreUrl + PushFilePath + '?' + sha1;
                        HttpResponseMessage r = _client.PostAsync( url, c ).GetAwaiter().GetResult();
                        r.Dispose();
                    }
                }
                return true;
            }
            catch( Exception ex )
            {
                monitor.Error( "Client call error.", ex );
                return false;
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }


    }
}
