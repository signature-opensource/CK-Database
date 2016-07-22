using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKLevel0
{
    [SqlPackage( ResourcePath ="Res", Schema = "CK")]
    [Versions("0.0.0")]
    public abstract class Package : SqlPackage
    {
        public string DynamicPropertySample => $"HashCode = {GetHashCode()}";

        [SqlProcedure( "sSimpleY4TemplateTest" )]
        public abstract string SimplY4TemplateTest( ISqlCallContext ctx );
        
        [SqlProcedure( "define:sSimpleReplaceTest" )]
        public abstract string SimpleReplaceTest( ISqlCallContext ctx, string textParam );
        
        // "define:" prefix is optional.
        [SqlProcedure( "sSimpleTransformTest" )]
        public abstract string SimpleTransormTest( ISqlCallContext ctx );
    }
}
