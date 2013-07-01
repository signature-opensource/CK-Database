using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using CK.Core;
using System.Reflection;

namespace CK.Setup
{
    class RawOutStream
    {
        public readonly MemoryStream Memory;
        public readonly BinaryWriter Writer;
        public readonly IFormatter Formatter;

        public RawOutStream()
        {
            Memory = new MemoryStream();
            Writer = new BinaryWriter( Memory );
            Formatter = new BinaryFormatter();
        }
    }
}
