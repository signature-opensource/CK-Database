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

        protected new TestAutoHeaderSPMemberAttribute Attribute
        {
            get { return (TestAutoHeaderSPMemberAttribute)base.Attribute; }
        }

        bool ISetupItemDriverAware.OnDriverCreated( GenericItemSetupDriver driver )
        {
            new TestAutoHeaderSPHandler( driver.Engine.Drivers[SetupObjectItem], Attribute.HeaderComment );
            return true;
        }
    }
}
