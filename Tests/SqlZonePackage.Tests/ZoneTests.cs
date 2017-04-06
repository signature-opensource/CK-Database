using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using SqlActorPackage.Basic;

namespace SqlZonePackage.Tests
{
    [TestFixture]
    public class ZoneTests
    {
        [Test]
        public void auto_header_injection_by_attribute_on_member()
        {
            var a = TestHelper.StObjMap.Default.Obtain<ActorHome>();

            var textA = a.Database.GetObjectDefinition( "CK.sUserToBeOverriden" );
            Assert.That( textA, Does.StartWith( 
                "--Injected from SqlZonePackage.Zone.UserHome.CmdUserToBeOverriden (n°2/2)."
                + Environment.NewLine
                + "--Injected from UserHome.CmdUserToBeOverriden (n°1/2)." ) );

            var textB = a.Database.GetObjectDefinition( "CK.sUserToBeOverridenIndirect" );
            Assert.That( textB, Does.StartWith( 
                "--Injected from SqlZonePackage.Zone.Package.TestAutoHeaderSP attribute (n°2/2)."
                + Environment.NewLine
                + "--Injected from UserHome.CmdUserToBeOverridenIndirect (n°1/2)." ) );

        }

        [Test]
        public void construct_injection_of_unresolved_AmbientContract_is_null()
        {
            var a = TestHelper.StObjMap.Default.Obtain<Package>();
            Assert.That(a.UnexistingByConstructParam, Is.Null);
        }

        [Test]
        public void optional_property_InjectContract_of_resolved_AmbientContract()
        {
            var a = TestHelper.StObjMap.Default.Obtain<Package>();
            Assert.That(a.ZoneHome, Is.SameAs(TestHelper.StObjMap.Default.Obtain<Zone.SecurityZoneHome>()));
            Assert.That(a.UnexistingByInjectContract, Is.Null, "Remains null.");
        }

        [Test]
        public void Initialize_method_provides_a_way_to_register_multiple_services()
        {
            var a = TestHelper.StObjMap.Default.Obtain<Package>();
            Assert.That(a.AllServices.Count, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new object[] 
            {
                TestHelper.StObjMap.Default.Obtain<Zone.GroupHome>(),
                TestHelper.StObjMap.Default.Obtain<Zone.Package>()
            }, a.AllServices );
        }

        [Test]
        public void checking_final_mappings()
        {
            IContextualStObjMap map = TestHelper.StObjMap.Default;
            var mappings = new List<KeyValuePair<Type, object>>();
            foreach (var t in map.Types) mappings.Add(new KeyValuePair<Type, object>(t,map.Obtain(t)));
            var mappings2 = map.StObjs.Select(d => new KeyValuePair<Type, object>(d.StObj.ObjectType, d.Implementation)).ToList();
            CollectionAssert.AreEquivalent(mappings, mappings2);
        }

    }
}
