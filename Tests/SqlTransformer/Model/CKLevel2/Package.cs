using CK.Core;
using CK.SqlServer;

namespace CKLevel2
{
    [SqlPackage( ResourcePath = "Res", Schema = "CK" )]
    [Versions("0.0.0")]
    public abstract class Package : CKLevel0.Package
    {
        [SqlProcedure( "replace:sSimpleReplaceTest" )]
        public abstract string SimpleReplaceTest( ISqlCallContext ctx, string textParam, int added );

        [SqlProcedure( "transform:sSimpleTransformTest" )]
        public abstract string SimpleTransformTest( ISqlCallContext ctx, string textParam, int added );
    }
}
