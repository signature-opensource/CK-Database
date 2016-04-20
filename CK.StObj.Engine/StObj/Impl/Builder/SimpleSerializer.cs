using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Simple serializer that can write data for <see cref="SimpleDeserializer"/>.
    /// </summary>
    public class SimpleSerializer
    {
        readonly BinaryWriter _writer;

        /// <summary>
        /// Initializes a new serializer on a <see cref="Stream"/>.
        /// </summary>
        /// <param name="s">The stream to use.</param>
        public SimpleSerializer( Stream s )
            : this( new BinaryWriter( s ) )
        {
        }

        /// <summary>
        /// Initializes a new serializer on a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="w">The writer to use.</param>
        public SimpleSerializer( BinaryWriter w )
        {
            _writer = w;
        }

        /// <summary>
        /// Writes an object value whose type must be in <see cref="SimpleDeserializer.KnownTypes"/>.
        /// </summary>
        /// <param name="o">The object to write.</param>
        public void Write( object o )
        {
            if( o == null )
            {
                WriteNull();
                return;
            }
            Type oT = o.GetType();
            if( oT.IsValueType )
            {
                if( o is bool ) Write( (bool)o );
                else if( o is int ) Write( (int)o );
                else if( o is long ) Write( (long)o );
                else if( o is short ) Write( (short)o );
                else if( o is sbyte ) Write( (sbyte)o );
                else if( o is uint ) Write( (uint)o );
                else if( o is ulong ) Write( (ulong)o );
                else if( o is ushort ) Write( (ushort)o );
                else if( o is byte ) Write( (byte)o );
                else if( o is Guid ) Write( (Guid)o );
                else if( o is char ) Write( (char)o );
                else if( o is double ) Write( (double)o );
                else if( o is float ) Write( (float)o );
                else if( o is decimal ) Write( (decimal)o );
                else if( o is DateTime ) Write( (DateTime)o );
                else if( o is TimeSpan ) Write( (TimeSpan)o );
                else if( o is DateTimeOffset ) Write( (DateTimeOffset)o );
                else throw new ArgumentException( "Unknown value type: " + oT.AssemblyQualifiedName );
                return;
            }
            string s = o as string;
            if( s != null ) Write( s );
            else
            {
                Type t = o as Type;
                if( t != null ) Write( t );
                else
                {
                    IEnumerable e = o as IEnumerable;
                    if( e != null )
                    {
                        int count;
                        ICollection c = o as ICollection;
                        if( c != null ) count = c.Count;
                        else 
                        {
                            count = 0;
                            foreach( var x in e ) ++count;
                        }
                        Write( e, count );
                    }
                    else throw new ArgumentException( "Unknown type: " + oT.AssemblyQualifiedName );
                }
            }
        }

        /// <summary>
        /// Writes a null reference.
        /// </summary>
        public void WriteNull()
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Null );
        }

        /// <summary>
        /// Writes a boolean.
        /// </summary>
        /// <param name="b">The value to write.</param>
        public void Write( bool b )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Bool );
            _writer.Write( b );
        }

        /// <summary>
        /// Writes a string that can be null (<see cref="WriteNull"/> will be called).
        /// </summary>
        /// <param name="s">The value to write.</param>
        public void Write( string s )
        {
            if( s == null ) WriteNull();
            else
            {
                _writer.Write( (byte)SimpleDeserializer.KnownTypes.String );
                _writer.Write( s );
            }
        }

        /// <summary>
        /// Writes a sbyte.
        /// </summary>
        /// <param name="value">The value to write.</param>

        [CLSCompliant( false )]
        public void Write( sbyte value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Int8 );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes a short.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( short value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Int16 );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes a integer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( int value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Int32 );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes a long.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( long value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Int64 );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes a byte.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( byte value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.UInt8 );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes an unsigned short.
        /// </summary>
        /// <param name="value">The value to write.</param>
        [CLSCompliant(false)]
        public void Write( ushort value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.UInt16 );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes an unsigned integer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        [CLSCompliant( false )]
        public void Write( uint value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.UInt32 );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes an unsigned long.
        /// </summary>
        /// <param name="value">The value to write.</param>
        [CLSCompliant( false )]
        public void Write( ulong value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.UInt64 );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes a char.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( char value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Char );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes a double.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( double value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Double );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes a float.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( float value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Float );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes a decimal.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( decimal value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Decimal );
            _writer.Write( value );
        }

        /// <summary>
        /// Writes a guid
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( Guid value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Guid );
            _writer.Write( value.ToByteArray() );
        }

        /// <summary>
        /// Writes a DateTime.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( DateTime value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Datetime );
            _writer.Write( value.ToBinary() );
        }

        /// <summary>
        /// Writes a TimeSpan.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( TimeSpan value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.TimeSpan );
            _writer.Write( value.Ticks );
        }

        /// <summary>
        /// Writes a DateTimeOffset.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write( DateTimeOffset value )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.DatetimeOffset );
            _writer.Write( value.DateTime.ToBinary() );
            _writer.Write( value.Offset.Ticks );
        }

        /// <summary>
        /// Writes a Type: its assembly qualified name is written.
        /// </summary>
        /// <param name="t">The Type to write.</param>
        public void Write( Type t )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.Type );
            _writer.Write( t.AssemblyQualifiedName );
        }

        /// <summary>
        /// Writes a list of object.
        /// </summary>
        /// <param name="e">The list to write.</param>
        /// <param name="count">The count of objects to write.</param>
        public void Write( IEnumerable e, int count )
        {
            _writer.Write( (byte)SimpleDeserializer.KnownTypes.ObjectList );
            _writer.Write( count );
            foreach( var o in e )
            {
                if( --count < 0 ) break;
                Write( o );
            }
        }
    }

}
