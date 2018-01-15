using CK.Core;
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
            var a = TestHelper.StObjMap.Default.Obtain<ActorHome>();

            var textA = a.Database.GetObjectDefinition( "CK.sActorCreate" );
            Assert.That( textA, Does.Contain( "--Injected From ActorHome - TestAutoHeaderAttribute." ) );

            var textB = a.Database.GetObjectDefinition( "CK.sActorGuidRefTest" );
            Assert.That( textB, Does.Contain( "--Injected From ActorHome - TestAutoHeaderAttribute." ) );
        }

        [Test]
        public void auto_header_injection_by_attribute_on_member()
        {
            var a = TestHelper.StObjMap.Default.Obtain<ActorHome>();

            var text = a.Database.GetObjectDefinition("CK.sActorGuidRefTest");
            Assert.That(text, Does.Contain("--Injected From CmdGuidRefTest - TestAutoHeaderSPMember."));
        }

        [Test]
        public void construct_injection_of_unresolved_AmbientContract_is_null()
        {
            var a = TestHelper.StObjMap.Default.Obtain<Package>();
            Assert.That(a.UnexistingByConstructParam, Is.Null);
        }

        [Test]
        public void optional_property_InjectContract_of_unresolved_AmbientContract_is_null()
        {
            var a = TestHelper.StObjMap.Default.Obtain<Package>();
            Assert.That(a.ZoneHome, Is.Null);
            Assert.That(a.UnexistingByInjectContract, Is.Null);
        }

        [Test]
        public void Initialize_method_provides_a_way_to_register_multiple_services()
        {
            var a = TestHelper.StObjMap.Default.Obtain<Package>();
            Assert.That(a.AllServices.Count, Is.EqualTo(1));
            Assert.That(a.AllServices[0], Is.SameAs(TestHelper.StObjMap.Default.Obtain<GroupHome>()));
        }
    }

}
