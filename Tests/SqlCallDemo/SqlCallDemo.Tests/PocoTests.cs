using CK.Core;
using CK.SqlServer;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.SqlServerTestHelper;

namespace SqlCallDemo.Tests;

[TestFixture]
public class PocoTests
{
    [Test]
    public void calling_base_Write_actually_take_the_Poco_class()
    {
        var factoryThing = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IThing>>();
        var factoryThingAH = SharedEngine.Map.StObjs.Obtain<IPocoFactory<PocoSupport.IThingWithAgeAndHeight>>();
        var p = SharedEngine.Map.StObjs.Obtain<PocoPackage>();
        Throw.DebugAssert( factoryThing != null && factoryThingAH != null && p != null );

        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
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
    public void reading_Poco_Thing_from_database()
    {
        var p = SharedEngine.Map.StObjs.Obtain<PocoPackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var thing = p.ReadFromDatabase( ctx );
            thing.Should().NotBeNull()
                    .And.BeAssignableTo<PocoSupport.IThingWithAgeAndHeight>()
                    .And.BeAssignableTo<PocoSupport.IThingWithPower>()
                    .And.BeAssignableTo<PocoSupport.IThingIntProp>();
            thing.Name.Should().Be( "ReadFromDatabase" );
            thing.UniqueId.Should().NotBe( Guid.Empty );
            var p1 = (PocoSupport.IThingWithAgeAndHeight)thing;
            p1.Age.Should().Be( 12 );
            p1.Height.Should().Be( 154 );
            var p2 = (PocoSupport.IThingWithPower)thing;
            p2.Power.Should().Be( 872 );
            var p3 = (PocoSupport.IThingIntProp)thing;
            p3.IntProp.Should().Be( 3712 );
        }
    }


    [Test]
    public async Task reading_Poco_Thing_from_database_Async()
    {
        var p = SharedEngine.Map.StObjs.Obtain<PocoPackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var thing = await p.ReadFromDatabaseAsync( ctx );
            thing.Should().NotBeNull()
                    .And.BeAssignableTo<PocoSupport.IThingWithAgeAndHeight>()
                    .And.BeAssignableTo<PocoSupport.IThingWithPower>()
                    .And.BeAssignableTo<PocoSupport.IThingIntProp>();
            thing.Name.Should().Be( "ReadFromDatabase" );
            thing.UniqueId.Should().NotBe( Guid.Empty );
            var p1 = (PocoSupport.IThingWithAgeAndHeight)thing;
            p1.Age.Should().Be( 12 );
            p1.Height.Should().Be( 154 );
            var p2 = (PocoSupport.IThingWithPower)thing;
            p2.Power.Should().Be( 872 );
            var p3 = (PocoSupport.IThingIntProp)thing;
            p3.IntProp.Should().Be( 3712 );
        }
    }
}
