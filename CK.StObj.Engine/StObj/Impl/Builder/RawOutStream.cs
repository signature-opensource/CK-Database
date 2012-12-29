using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CK.Setup
{

    enum KOpCode : byte
    {
        NOP,
        PushLogger,
        PushStObj,
        PushValue,
        PushCall
    }


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

        public void WriteCode( KOpCode c )
        {
            Memory.WriteByte( (byte)c );
        }

        public void Serialize( object o )
        {
            Formatter.Serialize( Memory, o );
        }

        public void WriteRef( MutableItem o )
        {
            Writer.Write( o.SpecializationIndexOrdered );
            Writer.Flush();
        }

    }
}
