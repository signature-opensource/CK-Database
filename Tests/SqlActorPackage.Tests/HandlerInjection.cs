using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using SqlActorPackage.Basic;

namespace SqlActorPackage.Tests
{
    [TestFixture]
    public class HandlerInjection
    {
        [Test]
        public void auto_header_injection_by_attribute()
        {
            var a = TestHelper.StObjMap.Default.Obtain<ActorHome>();
            
            var textA = a.Database.GetObjectDefinition( "CK.sActorCreate" );
            Assert.That( textA, Is.StringContaining( "--Injected From ActorHome - TestAutoHeaderAttribute." ) );

            var textB = a.Database.GetObjectDefinition( "CK.sActorGuidRefTest" );
            Assert.That( textB, Is.StringContaining( "--Injected From ActorHome - TestAutoHeaderAttribute." ) );
        }
    }
}
