using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class PocoTests
    {
        [Test]
        public void calling_base_Write_actually_take_the_Poco_class()
        {
            var factoryThing = TestHelper.StObjMap.StObjs.Obtain<IPocoFactory<IThing>>();
            var factoryThingAH = TestHelper.StObjMap.StObjs.Obtain<IPocoFactory<PocoSupport.IThingWithAgeAndHeight>>();
            var p = TestHelper.StObjMap.StObjs.Obtain<PocoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var t = factoryThing.Create( o => o.Name = "a thing" );
                Assert.That( p.Write( ctx, t ), Is.EqualTo( "a thing P=0 A,H=0,0" ) );
                var tAH = factoryThingAH.Create( o =>
                {
                    o.Name = "aged thing";
                    o.Age = 12;
                    o.Height = 170;
                } );
                Assert.That( p.Write( ctx, tAH ), Is.EqualTo( "aged thing P=0 A,H=12,170" ) );
            }
        }

        [Test]
        public void reading_Poco_Thing()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<PocoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var thing = p.Read( ctx );
                Assert.That( thing, Is.Not.Null );
            }
        }

        [Test]
        public void reading_Poco_Thing_from_database()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<PocoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var thing = p.ReadFromDatabase( ctx );
                thing.Should().NotBeNull()
                        .And.BeAssignableTo<PocoSupport.IThingWithAgeAndHeight>()
                        .And.BeAssignableTo<PocoSupport.IThingWithPower>()
                        .And.BeAssignableTo<PocoSupport.IThingReadOnlyProp>();
                thing.Name.Should().Be( "ReadFromDatabase" );
                thing.FromBatabaseOnly.Should().NotBe( Guid.Empty );
                var p1 = (PocoSupport.IThingWithAgeAndHeight)thing;
                p1.Age.Should().Be( 12 );
                p1.Height.Should().Be( 154 );
                var p2 = (PocoSupport.IThingWithPower)thing;
                p2.Power.Should().Be( 872 );
                var p3 = (PocoSupport.IThingReadOnlyProp)thing;
                p3.ReadOnlyProp.Should().Be( 3712 );
            }
        }


        [Test]
        public async Task reading_Poco_Thing_from_database_async()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<PocoPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var thing = await p.ReadFromDatabaseAsync( ctx );
                thing.Should().NotBeNull()
                        .And.BeAssignableTo<PocoSupport.IThingWithAgeAndHeight>()
                        .And.BeAssignableTo<PocoSupport.IThingWithPower>()
                        .And.BeAssignableTo<PocoSupport.IThingReadOnlyProp>();
                thing.Name.Should().Be( "ReadFromDatabase" );
                thing.FromBatabaseOnly.Should().NotBe( Guid.Empty );
                var p1 = (PocoSupport.IThingWithAgeAndHeight)thing;
                p1.Age.Should().Be( 12 );
                p1.Height.Should().Be( 154 );
                var p2 = (PocoSupport.IThingWithPower)thing;
                p2.Power.Should().Be( 872 );
                var p3 = (PocoSupport.IThingReadOnlyProp)thing;
                p3.ReadOnlyProp.Should().Be( 3712 );
            }
        }
    }
}
