using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Pretty simple deserializer to read injected values. 
    /// Supports a limited set of types: <see cref="KnownTypes"/>.
    /// </summary>
    public class SimpleDeserializer
    {
        readonly BinaryReader _reader;

        /// <summary>
        /// Defines the known types.
        /// </summary>
        public enum KnownTypes
        {
            /// <summary>
            /// The null value.
            /// </summary>
            Null = 0,
            /// <summary>
            /// A boolean value.
            /// </summary>
            Bool = 1,
            /// <summary>
            /// A string.
            /// </summary>
            String = 2,
            /// <summary>
            /// A sbyte.
            /// </summary>
            Int8 = 3,
            /// <summary>
            /// a short.
            /// </summary>
            Int16 = 4,
            /// <summary>
            /// An integer.
            /// </summary>
            Int32 = 5,
            /// <summary>
            /// A long.
            /// </summary>
            Int64 = 6,
            /// <summary>
            /// A byte.
            /// </summary>
            UInt8 = 7,
            /// <summary>
            /// An unsigned short.
            /// </summary>
            UInt16 = 8,
            /// <summary>
            /// An unsigned integer.
            /// </summary>
            UInt32 = 9,
            /// <summary>
            /// An unsigned long.
            /// </summary>
            UInt64 = 10,
            /// <summary>
            /// A character.
            /// </summary>
            Char = 11,
            /// <summary>
            /// A double.
            /// </summary>
            Double = 12,
            /// <summary>
            /// A float.
            /// </summary>
            Float = 13,
            /// <summary>
            /// A decimal.
            /// </summary>
            Decimal = 14,
            /// <summary>
            /// A Guid.
            /// </summary>
            Guid = 15,
            /// <summary>
            /// A datetime.
            /// </summary>
            Datetime = 16,
            /// <summary>
            /// A timespan.
            /// </summary>
            TimeSpan = 17,
            /// <summary>
            /// A DateTimeOffset.
            /// </summary>
            DatetimeOffset = 18,
            /// <summary>
            /// A Type.
            /// </summary>
            Type = 19,
            /// <summary>
            /// A list of object, each of them beeing a known.
            /// This is read back as an array, but can be written as any IEnumerable or, better 
            /// (since length is known), any ICollection.
            /// </summary>
            ObjectList = 20,
             /// <summary>
            /// The <see cref="Type.Missing"/> marker.
            /// </summary>
            TypeMissing = 21
        }

        /// <summary>
        /// Encapsulates a read value with its type.
        /// </summary>
        public struct ReadValue
        {
            /// <summary>
            /// The type of the value.
            /// </summary>
            public readonly KnownTypes Type;

            /// <summary>
            /// The value itself.
            /// </summary>
            public readonly object Value;

            /// <summary>
            /// Initializes a new value with its type.
            /// </summary>
            /// <param name="t"></param>
            /// <param name="v"></param>
            public ReadValue( KnownTypes t, object v )
            {
                Type = t;
                Value = v;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="SimpleDeserializer"/> on a stream.
        /// </summary>
        /// <param name="s">The stream to read.</param>
        public SimpleDeserializer( Stream s )
        {
            _reader = new BinaryReader( s );
        }

        /// <summary>
        /// Reads the next value.
        /// </summary>
        /// <param name="readArrayOfReadValue">
        /// True to return an array of <see cref="ReadValue"/> instead
        /// of object when reading <see cref="KnownTypes.ObjectList"/>.
        /// By default, array of value objects is returned (the read type is lost).
        /// </param>
        /// <returns>The read object.</returns>
        public ReadValue Read( bool readArrayOfReadValue = false )
        {
            var m = (KnownTypes)_reader.ReadByte();
            switch( m )
            {
                case KnownTypes.Null: return new ReadValue();
                case KnownTypes.Bool: return new ReadValue( m, _reader.ReadByte() == 1 );
                case KnownTypes.String: return new ReadValue( m, _reader.ReadString() );
                case KnownTypes.Int8: return new ReadValue( m, _reader.ReadSByte() );
                case KnownTypes.Int16: return new ReadValue( m, _reader.ReadInt16() );
                case KnownTypes.Int32: return new ReadValue( m, _reader.ReadInt32() );
                case KnownTypes.Int64: return new ReadValue( m, _reader.ReadInt64() );
                case KnownTypes.UInt8: return new ReadValue( m, _reader.ReadByte() );
                case KnownTypes.UInt16: return new ReadValue( m, _reader.ReadUInt16() );
                case KnownTypes.UInt32: return new ReadValue( m, _reader.ReadUInt32() );
                case KnownTypes.UInt64: return new ReadValue( m, _reader.ReadUInt64() );
                case KnownTypes.Char: return new ReadValue( m, _reader.ReadChar() );
                case KnownTypes.Double: return new ReadValue( m, _reader.ReadDouble() );
                case KnownTypes.Float: return new ReadValue( m, _reader.ReadSingle() );
                case KnownTypes.Decimal: return new ReadValue( m, _reader.ReadDecimal() );
                case KnownTypes.Guid: return new ReadValue( m, new Guid( _reader.ReadBytes( 16 ) ) );
                case KnownTypes.Datetime: return new ReadValue( m, DateTime.FromBinary( _reader.ReadInt64() ) );
                case KnownTypes.TimeSpan: return new ReadValue( m, TimeSpan.FromTicks( _reader.ReadInt64() ) );
                case KnownTypes.DatetimeOffset: return new ReadValue( m, new DateTimeOffset( _reader.ReadInt64(), TimeSpan.FromTicks( _reader.ReadInt64() ) ) );
                case KnownTypes.Type: return new ReadValue( m, SimpleTypeFinder.WeakResolver( _reader.ReadString(), true ) );
                case KnownTypes.TypeMissing: return new ReadValue(m, Type.Missing);
                case KnownTypes.ObjectList:
                    {
                        int len = _reader.ReadInt32();
                        if( readArrayOfReadValue )
                        {
                            ReadValue[] r = new ReadValue[len];
                            for( int i = 0; i < len; ++i )
                            {
                                r[i] = Read( true );
                            }
                            return new ReadValue( m, r );
                        }
                        else
                        {
                            object[] r = new object[len];
                            for( int i = 0; i < len; ++i )
                            {
                                r[i] = Read( false ).Value;
                            }
                            return new ReadValue( m, r );
                        }
                    }
                default: throw new InvalidDataException( "Unknown marker value: " + (int)m );
            }
        }

    }
}
