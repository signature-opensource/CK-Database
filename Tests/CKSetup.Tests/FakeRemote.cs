using CK.Core;
using CKSetup.StreamStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.Tests
{
    class FakeRemote : IComponentImporter, IComponentPushTarget
    {
        readonly RuntimeArchive _remote;
        readonly IStreamStore _privateStore;
        readonly CompressionKind _privateStoreStorageKind;

        public FakeRemote( RuntimeArchive remote )
        {
            _remote = remote;
            _privateStore = (IStreamStore)typeof( RuntimeArchive ).GetField( "_store", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( remote );
            _privateStoreStorageKind = (CompressionKind)typeof( RuntimeArchive ).GetField( "_storageKind", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( remote );
        }

        public Stream OpenImportStream( IActivityMonitor monitor, ComponentMissingDescription missing )
        {
            var buffer = new MemoryStream();
            _remote.ExportComponents( missing, buffer, monitor );
            buffer.Position = 0;
            return buffer;
        }

        public StoredStream GetDownloadStream( IActivityMonitor monitor, SHA1Value file, CompressionKind preferred )
        {
            return _privateStore.OpenRead( file.ToString(), preferred );
        }

        PushComponentsResult IComponentPushTarget.PushComponents( IActivityMonitor monitor, Action<Stream> componentsWriter )
        {
            using( var m = new MemoryStream() )
            {
                componentsWriter( m );
                m.Position = 0;
                return _remote.ImportComponents( m );
            }
        }

        bool IComponentPushTarget.PushFile( IActivityMonitor monitor, string sessionId, SHA1Value sha1, Action<Stream> writer, CompressionKind kind )
        {
            monitor.Debug( $"Pushing {sha1}." );
            _privateStore.Update( sha1.ToString(), writer, kind, true );
            return true;
        }
    }

}
