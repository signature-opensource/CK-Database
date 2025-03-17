using CK.Core;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using SqlActorPackage.Basic;

namespace SqlActorPackage.Tests;

[TestFixture]
public class HandlerInjection
{
    [Test]
    public void auto_header_injection_by_attribute_on_class()
    {
        var a = SharedEngine.Map.StObjs.Obtain<ActorHome>();

        var textA = a.Database.GetObjectDefinition( "CK.sActorCreate" );
        textA.ShouldContain( "--Injected From ActorHome - TestAutoHeaderAttribute." );

        var textB = a.Database.GetObjectDefinition( "CK.sActorGuidRefTest" );
        textB.ShouldContain( "--Injected From ActorHome - TestAutoHeaderAttribute." );
    }

    [Test]
    public void auto_header_injection_by_attribute_on_member()
    {
        var a = SharedEngine.Map.StObjs.Obtain<ActorHome>();

        var text = a.Database.GetObjectDefinition( "CK.sActorGuidRefTest" );
        text.ShouldContain( "--Injected From CmdGuidRefTest - TestAutoHeaderSPMember." );
    }

    [Test]
    public void construct_injection_of_unresolved_RealObject_is_null()
    {
        var a = SharedEngine.Map.StObjs.Obtain<Package>();
        a.UnexistingByConstructParam.ShouldBeNull();
    }

    [Test]
    public void optional_property_InjectObject_of_unresolved_RealObject_is_null()
    {
        var a = SharedEngine.Map.StObjs.Obtain<Package>();
        a.ZoneHome.ShouldBeNull();
        a.UnexistingByInjectObject.ShouldBeNull();
    }

    [Test]
    public void Initialize_method_provides_a_way_to_register_multiple_services()
    {
        var a = SharedEngine.Map.StObjs.Obtain<Package>();
        a.AllServices.Count.ShouldBe( 1 );
        a.AllServices[0].ShouldBeSameAs( SharedEngine.Map.StObjs.Obtain<GroupHome>() );
    }
}
