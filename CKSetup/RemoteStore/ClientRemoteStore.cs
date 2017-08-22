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
using System.Net;
using System.Xml.Linq;
using System.Xml;

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
        static public readonly string PushFilePath = "/push/f";
        static public readonly string PullPath = "/pull";
        static public readonly string PullFilePath = "/pull/f";

        readonly HttpClient _client;
        readonly string _remotePrefix;
        readonly string _pushApiKey;

        public ClientRemoteStore( Uri remoteStoreUrl, string pushApiKey )
        {
            _client = new HttpClient();
            _remotePrefix = remoteStoreUrl + RootPathString.Substring( 1 );
            _pushApiKey = pushApiKey;
        }

        Stream IComponentImporter.OpenImportStream( IActivityMonitor monitor, ComponentMissingDescription missing )
        {
            try
            {
                using( var buffer = new MemoryStream() )
                {
                    using( var w = XmlWriter.Create( buffer, new XmlWriterSettings() { CloseOutput = false, Indent = false } ) )
                    {
                        missing.ToXml().WriteTo( w );
                    }
                    HttpResponseMessage r;
                    buffer.Position = 0;
                    using( var c = new StreamContent( buffer ) )
                    {
                        r = _client.PostAsync( _remotePrefix + PullPath, c ).GetAwaiter().GetResult();
                    }
                    return r.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                }
            }
            catch( Exception ex )
            {
                monitor.Error( "Client call error.", ex );
                return null;
            }
        }

        StoredStream IComponentFileDownloader.GetDownloadStream( IActivityMonitor monitor, SHA1Value file, CompressionKind kind )
        {
            var url = _remotePrefix + PullFilePath + '/' + file.ToString();
            return new StoredStream( CompressionKind.GZiped,
                                     _client.GetAsync( url ).GetAwaiter().GetResult()
                                        .Content.ReadAsStreamAsync().GetAwaiter().GetResult() );
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
                        using( HttpResponseMessage r = _client.PostAsync( _remotePrefix + PushPath, c ).GetAwaiter().GetResult() )
                        {
                            if( !r.IsSuccessStatusCode )
                            {
                                return new PushComponentsResult( $"Remote response Status: {r.StatusCode}.", null );
                            }
                            using( var responseStream = r.Content.ReadAsStreamAsync().GetAwaiter().GetResult() )
                            {
                                return new PushComponentsResult( new CKBinaryReader( responseStream ) );
                            }
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
                bool success = true;
                using( var buffer = new MemoryStream() )
                {
                    if( kind == CompressionKind.None ) writer = StreamStoreExtension.GetCompressShell( writer );
                    writer( buffer );
                    buffer.Position = 0;
                    using( var c = new StreamContent( buffer ) )
                    {
                        c.Headers.Add( SessionIdHeader, sessionId );
                        var url = _remotePrefix + PushFilePath + '/' + sha1;
                        using( HttpResponseMessage r = _client.PostAsync( url, c ).GetAwaiter().GetResult() )
                        {
                            if( !r.IsSuccessStatusCode )
                            {
                                monitor.Error( $"Remote response Status: {r.StatusCode}." );
                                success = false;
                            }
                        }
                    }
                }
                return success;
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
