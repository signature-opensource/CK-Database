using CK.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.StObjSetupTestHelper;

namespace CK.StObj.DependencyInjection.Tests
{
    [TestFixture]
    public class StObjSetupTests
    {
        [Test]
        public void running_TestHelper_RunStObjSetup()
        {
            var conf = new StObjEngineConfiguration();
            var r = TestHelper.RunStObjSetup( conf );
        }

    }
}
