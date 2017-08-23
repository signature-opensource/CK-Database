using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.StreamStore
{
    public struct StoredStream
    {
        public readonly CompressionKind Kind;
        public readonly Stream Stream;

        public StoredStream( CompressionKind k, Stream s )
        {
            Kind = k;
            Stream = s;
        }
    }
}
