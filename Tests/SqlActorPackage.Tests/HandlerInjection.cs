using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using SqlActorPackage.Basic;
using static CK.Testing.CKDatabaseLocalTestHelper;

namespace SqlActorPackage.Tests
{
    [TestFixture]
    public class HandlerInjection
    {
        [Test]
        public void auto_header_injection_by_attribute_on_class()
        {
            var a = TestHelper.StObjMap.StObjs.Obtain<ActorHome>();

            var textA = a.Database.GetObjectDefinition( "CK.sActorCreate" );
            textA.Should().Contain( "--Injected From ActorHome - TestAutoHeaderAttribute." );

            var textB = a.Database.GetObjectDefinition( "CK.sActorGuidRefTest" );
            textB.Should().Contain( "--Injected From ActorHome - TestAutoHeaderAttribute." );
        }

        [Test]
        public void auto_header_injection_by_attribute_on_member()
        {
            var a = TestHelper.StObjMap.StObjs.Obtain<ActorHome>();

            var text = a.Database.GetObjectDefinition("CK.sActorGuidRefTest");
            text.Should().Contain( "--Injected From CmdGuidRefTest - TestAutoHeaderSPMember." );
        }

        [Test]
        public void construct_injection_of_unresolved_RealObject_is_null()
        {
            var a = TestHelper.StObjMap.StObjs.Obtain<Package>();
            a.UnexistingByConstructParam.Should().BeNull();
        }

        [Test]
        public void optional_property_InjectObject_of_unresolved_RealObject_is_null()
        {
            var a = TestHelper.StObjMap.StObjs.Obtain<Package>();
            a.ZoneHome.Should().BeNull();
            a.UnexistingByInjectObject.Should().BeNull();
        }

        [Test]
        public void Initialize_method_provides_a_way_to_register_multiple_services()
        {
            var a = TestHelper.StObjMap.StObjs.Obtain<Package>();
            a.AllServices.Should().HaveCount( 1 );
            a.AllServices[0].Should().BeSameAs( TestHelper.StObjMap.StObjs.Obtain<GroupHome>() );
        }
    }

}
