using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection.Emit;
using System.Reflection;

namespace CK.SqlServer
{
    public class SqlAutoImplementAttribute : IAutoImplementorMethod
    {

        public SqlAutoImplementAttribute( string schemaName )
        {
            SchemaName = schemaName;
        }

        public string SchemaName { get; private set; }

        bool IAutoImplementorMethod.Implement( IActivityLogger logger, MethodInfo m, TypeBuilder b )
        {
            return true;
        }
    }
}
