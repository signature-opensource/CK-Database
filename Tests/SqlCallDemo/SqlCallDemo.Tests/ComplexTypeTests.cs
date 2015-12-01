using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using SqlCallDemo.ComplexType;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class ComplexTypeTests
    {
        [Test]
        public void getting_a_totally_stupid_empty_object()
        {
            var p = TestHelper.StObjMap.Default.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var o = p.GetComplexTypeStupidEmpty( ctx );
                Assert.That( o, Is.Not.Null );
                var o2 = p.GetComplexTypeStupidEmpty( ctx );
                Assert.That( o2, Is.Not.Null );
                Assert.That( o2, Is.Not.SameAs( o ) );
            }
        }

        [Test]
        public void getting_a_simple_complex_type()
        {
            var p = TestHelper.StObjMap.Default.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                {
                    var o = p.GetComplexTypeSimple( ctx );
                    Assert.That( o.Id, Is.EqualTo( 0 ) );
                    Assert.That( o.Name, Is.EqualTo( "The name...0" ) );
                    Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                    Assert.That( o.NullableInt, Is.Null );
                }
                {
                    var o = p.GetComplexTypeSimple( ctx, 1 );
                    Assert.That( o.Id, Is.EqualTo( 3712 ) );
                    Assert.That( o.Name, Is.EqualTo( "The name...3712" ) );
                    Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                    Assert.That( o.NullableInt.HasValue );
                    Assert.That( o.NullableInt.Value, Is.EqualTo( 1 ) );
                }
            }
        }

        [Test]
        public void getting_a_simple_complex_typeWithCtor()
        {
            var p = TestHelper.StObjMap.Default.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                {
                    var o = p.GetComplexTypeSimpleWithCtor( ctx );
                    Assert.That( o.Id, Is.EqualTo( 100000 ) );
                    Assert.That( o.Name, Is.EqualTo( "From Ctor: The name...0" ) );
                    Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                }
                {
                    var o = p.GetComplexTypeSimpleWithCtor( ctx, 1 );
                    Assert.That( o.Id, Is.EqualTo( 100000 + 3712 ) );
                    Assert.That( o.Name, Is.EqualTo( "From Ctor: The name...3712" ) );
                    Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                }
            }
        }

        [Test]
        public void getting_a_simple_complex_type_with_extra_property_is_fine()
        {
            var p = TestHelper.StObjMap.Default.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var o = p.GetComplexTypeSimpleWithExtraProperty( ctx );
                Assert.That( o.Id, Is.EqualTo( 0 ) );
                Assert.That( o.Name, Is.EqualTo( "The name...0" ) );
                Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                Assert.That( o.ExtraProperty, Is.Null );
            }
        }

        [Test]
        public void getting_a_simple_complex_type_with_missing_property_is_fine()
        {
            var p = TestHelper.StObjMap.Default.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var o = p.GetComplexTypeSimpleWithMissingProperty( ctx );
                Assert.That( o.Name, Is.EqualTo( "The name...0" ) );
            }
        }

    }
}
