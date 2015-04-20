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
    public class HandlerInjection
    {
        [Test]
        public void auto_header_injection_by_attribute_on_member()
        {
            var a = TestHelper.StObjMap.Default.Obtain<ActorHome>();

            var textA = a.Database.GetObjectDefinition( "CK.sUserToBeOverriden" );
            Assert.That( textA, Is.StringContaining( @"
-- Injected from UserHome.CmdUserToBeOverriden (n°1/2).

-- Injected from SqlZonePackage.Zone.UserHome.CmdUserToBeOverriden (n°2/2)." ) );

            var textB = a.Database.GetObjectDefinition( "CK.sUserToBeOverridenIndirect" );
            Assert.That( textB, Is.StringContaining( @"
-- Injected from UserHome.CmdUserToBeOverridenIndirect (n°1/2).

-- Injected from SqlZonePackage.Zone.Package.TestAutoHeaderSP attribute (n°2/2)." ) );

        }

    }
}
