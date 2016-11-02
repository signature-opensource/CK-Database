using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Runtime
{
    public class TestAutoHeaderSPMemberAttributeImpl : SetupObjectItemRefMemberAttributeImplBase, ISetupItemDriverAware
    {
        public TestAutoHeaderSPMemberAttributeImpl( TestAutoHeaderSPMemberAttribute a )
            : base( a )
        {
        }

        protected new TestAutoHeaderSPMemberAttribute Attribute => (TestAutoHeaderSPMemberAttribute)base.Attribute; 

        bool ISetupItemDriverAware.OnDriverPreInitialized( SetupItemDriver driver )
        {
            new TestAutoHeaderSPHandler( driver.Engine.Drivers[SetupObjectItem], Attribute.HeaderComment );
            return true;
        }
    }
}
