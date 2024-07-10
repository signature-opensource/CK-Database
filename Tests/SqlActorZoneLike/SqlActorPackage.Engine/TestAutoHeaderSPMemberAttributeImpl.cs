using CK.Setup;
using CK.Core;

namespace SqlActorPackage.Engine
{
    public class TestAutoHeaderSPMemberAttributeImpl : SetupObjectItemRefMemberAttributeImplBase, ISetupItemDriverAware
    {
        public TestAutoHeaderSPMemberAttributeImpl( TestAutoHeaderSPMemberAttribute a )
            : base( a )
        {
        }

        protected new TestAutoHeaderSPMemberAttribute Attribute => (TestAutoHeaderSPMemberAttribute)base.Attribute; 

        bool ISetupItemDriverAware.OnDriverPreInitialized( IActivityMonitor m, SetupItemDriver driver )
        {
            new TestAutoHeaderSPHandler( driver.Drivers[SetupObjectItem], Attribute.HeaderComment );
            return true;
        }
    }
}
