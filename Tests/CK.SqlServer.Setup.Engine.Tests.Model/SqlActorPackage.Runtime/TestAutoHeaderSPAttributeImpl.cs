using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Runtime
{
    public class TestAutoHeaderSPAttributeImpl : SetupItemSelectorBaseAttributeImpl<SqlProcedureItem>
    {
        public TestAutoHeaderSPAttributeImpl( TestAutoHeaderSPAttribute a )
            : base( a )
        {
        }

        protected new TestAutoHeaderSPAttribute Attribute
        {
            get { return (TestAutoHeaderSPAttribute)base.Attribute; }
        }

        protected override bool OnDriverCreated( GenericItemSetupDriver driver, IEnumerable<SqlProcedureItem> items )
        {
            foreach( var sp in items )
            {
                new TestAutoHeaderSPHandler( driver.Engine.Drivers[sp], Attribute.SqlHeader );
            }
            return true;
        }
    }
}
