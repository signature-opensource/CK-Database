//using CK.Core;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CK.StObj.Engine.Tests
//{
//    [TestFixture]
//    public class SimpleSerializerDeserializerTests
//    {
//        [Test]
//        public void writing_and_reading_simple_values_of_all_known_types()
//        {
//            DateTime tD = DateTime.Now;
//            DateTimeOffset tO = DateTimeOffset.Now;
//            TimeSpan tT = TimeSpan.FromMilliseconds( 987897689 );
//            Guid g = Guid.NewGuid();

//            MemoryStream mem = new MemoryStream();
//            SimpleSerializer s = new SimpleSerializer( mem );
//            s.WriteNull();
//            s.Write( true );
//            s.Write( false );
//            s.Write( "Hello World!" );
//            s.Write( (sbyte)1 );
//            s.Write( (short)2 );
//            s.Write( 3 );
//            s.Write( (long)4 );
//            s.Write( (byte)5 );
//            s.Write( (ushort)6 );
//            s.Write( (uint)7 );
//            s.Write( (ulong)8 );
//            s.Write( 'a' );
//            s.Write( Math.E );
//            s.Write( (float)Math.E );
//            s.Write( new decimal( Math.E ) );
//            s.Write( g );
//            s.Write( tD );
//            s.Write( tO );
//            s.Write( tT );
//            s.Write( System.Type.Missing );
//            s.Write( GetType() );

//            mem.Position = 0;
//            SimpleDeserializer d = new SimpleDeserializer( mem );
//            HashSet<int> types = new HashSet<int>();
//            List<object> allValues = new List<object>();
//            CheckAllSimpleValues( tD, tO, tT, g, d, types, allValues );

//            var allKnownTypes = Enum.GetValues( typeof( SimpleDeserializer.KnownTypes ) ).Cast<int>();
//            CollectionAssert.AreEquivalent( allKnownTypes.Except( types ), new[] { (int)SimpleDeserializer.KnownTypes.ObjectList } );

//            // Reuse allValues to test Write( object ).
//            mem = new MemoryStream();
//            s = new SimpleSerializer( mem );
//            foreach( var o in allValues ) s.Write( o ); 
//            mem.Position = 0;
//            d = new SimpleDeserializer( mem );
//            CheckAllSimpleValues( tD, tO, tT, g, d, types, null );
//        }

//        private void CheckAllSimpleValues( DateTime tD, DateTimeOffset tO, TimeSpan tT, Guid g, SimpleDeserializer d, HashSet<int> types, List<object> allValues )
//        {
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Null, null, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Bool, true, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Bool, false, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.String, "Hello World!", types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Int8, 1, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Int16, 2, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Int32, 3, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Int64, 4, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.UInt8, 5, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.UInt16, 6, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.UInt32, 7, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.UInt64, 8, types, allValues );

//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Char, 'a', types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Double, Math.E, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Float, (float)Math.E, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Decimal, new decimal( Math.E ), types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Guid, g, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.Datetime, tD, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.DatetimeOffset, tO, types, allValues );
//            CheckSimpleValue( d, SimpleDeserializer.KnownTypes.TimeSpan, tT, types, allValues );
//            CheckSimpleValue(d, SimpleDeserializer.KnownTypes.TypeMissing, System.Type.Missing, types, allValues);
//            CheckSimpleValue(d, SimpleDeserializer.KnownTypes.Type, GetType(), types, allValues);
//        }

//        void CheckSimpleValue( 
//            SimpleDeserializer d, 
//            SimpleDeserializer.KnownTypes t, 
//            object value, 
//            HashSet<int> types,
//            List<object> allValues )
//        {
//            var r = d.Read();
//            Assert.That( r.Type, Is.EqualTo( t ) );
//            Assert.That( r.Value, Is.EqualTo( value ) );
//            types.Add( (int)t );
//            if( allValues != null ) allValues.Add( r.Value );
//        }

//        [Test]
//        public void writing_and_reading_array_of_objects()
//        {
//            object[] data = new object[] { 1, 8, null, "Hello", Math.E };
//            MemoryStream mem = new MemoryStream();
//            SimpleSerializer s = new SimpleSerializer( mem );
//            s.Write( data, data.Length );

//            mem.Position = 0;
//            SimpleDeserializer d = new SimpleDeserializer( mem );
//            var r = d.Read();
//            Assert.That( r.Type, Is.EqualTo( SimpleDeserializer.KnownTypes.ObjectList ) );
//            CollectionAssert.AreEqual( (object[])r.Value, data );
//        }

//        [Test]
//        public void writing_and_reading_recursive_list_of_objects()
//        {
//            object[] data = new object[] { 1, (byte)8, null, new object[] { "Hello", 87, "World" }, Math.E };
//            MemoryStream mem = new MemoryStream();
//            SimpleSerializer s = new SimpleSerializer( mem );
//            s.Write( data, data.Length );
//            mem.Position = 0;
//            SimpleDeserializer d = new SimpleDeserializer( mem );
//            var r = d.Read();
//            Assert.That( r.Type, Is.EqualTo( SimpleDeserializer.KnownTypes.ObjectList ) );
//            CollectionAssert.AreEqual( (object[])r.Value, data );
//        }

//        [Test]
//        public void writing_an_unknown_typed_object_is_an_error()
//        {
//            MemoryStream mem = new MemoryStream();
//            SimpleSerializer s = new SimpleSerializer( mem );
//            Assert.Throws<ArgumentException>( () => s.Write( this ) );
//        }
//    }
//}
