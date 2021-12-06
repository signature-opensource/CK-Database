using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using SqlCallDemo.ComplexType;
using System;
using System.Threading.Tasks;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class ComplexTypeAsyncTests
    {
        [Test]
        public async Task getting_a_totally_stupid_empty_object_Async()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var o = await p.GetComplexTypeStupidEmptyAsync( ctx ).ConfigureAwait( false );
                Assert.That( o, Is.Not.Null );
                var o2 = await p.GetComplexTypeStupidEmptyAsync( ctx ).ConfigureAwait( false );
                Assert.That( o2, Is.Not.Null );
                Assert.That( o2, Is.Not.SameAs( o ) );
            }
        }

        [Test]
        public async Task getting_a_simple_complex_type_Async()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                {
                    var o = await p.GetComplexTypeSimpleAsync( ctx ).ConfigureAwait( false );
                    Assert.That( o.Id, Is.EqualTo( 0 ) );
                    Assert.That( o.Name, Is.EqualTo( "The name...0" ) );
                    Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                    Assert.That( o.NullableInt, Is.Null );
                }
                {
                    var o = await p.GetComplexTypeSimpleAsync( ctx, 1 ).ConfigureAwait( false );
                    Assert.That( o.Id, Is.EqualTo( 3712 ) );
                    Assert.That( o.Name, Is.EqualTo( "The name...3712" ) );
                    Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                    Assert.That( o.NullableInt.HasValue );
                    Assert.That( o.NullableInt.Value, Is.EqualTo( 1 ) );
                }
            }
        }

        [Test]
        public async Task getting_a_simple_complex_typeWithCtor_Async()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                {
                    var o = await p.GetComplexTypeSimpleWithCtorAsync( ctx ).ConfigureAwait( false );
                    Assert.That( o.Id, Is.EqualTo( 100000 ) );
                    Assert.That( o.Name, Is.EqualTo( "From Ctor: The name...0" ) );
                    Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                }
                {
                    var o = await p.GetComplexTypeSimpleWithCtorAsync( ctx, 1 ).ConfigureAwait( false );
                    Assert.That( o.Id, Is.EqualTo( 100000 + 3712 ) );
                    Assert.That( o.Name, Is.EqualTo( "From Ctor: The name...3712" ) );
                    Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                }
            }
        }

        [Test]
        public async Task getting_a_simple_complex_type_with_extra_property_is_fine_Async()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var o = await p.GetComplexTypeSimpleWithExtraPropertyAsync( ctx ).ConfigureAwait( false );
                Assert.That( o.Id, Is.EqualTo( 0 ) );
                Assert.That( o.Name, Is.EqualTo( "The name...0" ) );
                Assert.That( o.CreationDate, Is.GreaterThan( DateTime.UtcNow.AddSeconds( -1 ) ).And.LessThan( DateTime.UtcNow.AddSeconds( 1 ) ) );
                Assert.That( o.ExtraProperty, Is.Null );
            }
        }

        [Test]
        public async Task getting_a_simple_complex_type_with_missing_property_is_fine_Async()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<ComplexTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var o = await p.GetComplexTypeSimpleWithMissingPropertyAsync( ctx ).ConfigureAwait( false );
                Assert.That( o.Name, Is.EqualTo( "The name...0" ) );
            }
        }

    }
}
