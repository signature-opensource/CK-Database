using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using SqlActorPackage.Basic;
using System;
using System.Collections.Generic;
using static CK.Testing.DBSetupTestHelper;

namespace SqlZonePackage.Tests
{
    [TestFixture]
    public class ZoneTests
    {
        [Test]
        public void auto_header_injection_by_attribute_on_member()
        {
            var a = TestHelper.StObjMap.StObjs.Obtain<ActorHome>();

            var textA = a.Database.GetObjectDefinition( "CK.sUserToBeOverriden" );
            textA.Should().StartWith( 
                "--Injected from SqlZonePackage.Zone.UserHome.CmdUserToBeOverriden (n°2/2)."
                + Environment.NewLine
                + "--Injected from UserHome.CmdUserToBeOverriden (n°1/2)." );

            var textB = a.Database.GetObjectDefinition( "CK.sUserToBeOverridenIndirect" );
            textB.Should().StartWith( 
                "--Injected from SqlZonePackage.Zone.Package.TestAutoHeaderSP attribute (n°2/2)."
                + Environment.NewLine
                + "--Injected from UserHome.CmdUserToBeOverridenIndirect (n°1/2)." );

        }

        [Test]
        public void construct_injection_of_unresolved_AmbientContract_is_null()
        {
            var a = TestHelper.StObjMap.StObjs.Obtain<Package>();
            Assert.That(a.UnexistingByConstructParam, Is.Null);
        }

        [Test]
        public void optional_property_InjectContract_of_resolved_AmbientContract()
        {
            var a = TestHelper.StObjMap.StObjs.Obtain<Package>();
            Assert.That(a.ZoneHome, Is.SameAs(TestHelper.StObjMap.StObjs.Obtain<Zone.SecurityZoneHome>()));
            Assert.That(a.UnexistingByInjectContract, Is.Null, "Remains null.");
        }

        [Test]
        public void Initialize_method_provides_a_way_to_register_multiple_services()
        {
            var a = TestHelper.StObjMap.StObjs.Obtain<Package>();
            Assert.That(a.AllServices.Count, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new object[] 
            {
                TestHelper.StObjMap.StObjs.Obtain<Zone.GroupHome>(),
                TestHelper.StObjMap.StObjs.Obtain<Zone.Package>()
            }, a.AllServices );
        }

        [Test]
        public void checking_final_mappings()
        {
            IStObjObjectMap map = TestHelper.StObjMap.StObjs;
            var mappings = new List<KeyValuePair<Type, object>>();
            foreach (var t in map.Types) mappings.Add(new KeyValuePair<Type, object>(t,map.Obtain(t)));
            CollectionAssert.AreEquivalent(mappings, map.Mappings);
        }

    }
}
