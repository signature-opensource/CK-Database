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
    class FakeRemote : IComponentImporter
    {
        readonly RuntimeArchive _remote;
        readonly IStreamStore _privateStore;

        public FakeRemote( RuntimeArchive remote )
        {
            _remote = remote;
            _privateStore = (IStreamStore)typeof( RuntimeArchive ).GetField( "_store", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( remote );
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

    }

}
