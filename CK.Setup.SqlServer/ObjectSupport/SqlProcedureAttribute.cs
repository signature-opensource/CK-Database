using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection.Emit;
using System.Reflection;

namespace CK.Setup.SqlServer
{
    public class SqlProcedureAttribute : IAutoImplementorMethod
    {
        public SqlProcedureAttribute( string schemaName )
        {
            SchemaName = schemaName;
        }

        public string SchemaName { get; private set; }

        bool IAutoImplementorMethod.Implement( IActivityLogger logger, MethodInfo m, TypeBuilder b )
        {
            CK.Reflection.EmitHelper.ImplementStubMethod( b, m, true );
            return true;
        }
    }
}
