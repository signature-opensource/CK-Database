using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using SqlActorPackage.Basic;
using System.Diagnostics;
using NUnit.Framework.Constraints;

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

            var text = a.Database.GetObjectDefinition( "CK.sActorGuidRefTest" );
            Assert.That( text, Does.Contain( "--Injected From CmdGuidRefTest - TestAutoHeaderSPMember." ) );
        }
    }

}
