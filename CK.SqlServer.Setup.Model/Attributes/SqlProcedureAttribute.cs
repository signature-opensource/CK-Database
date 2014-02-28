using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection.Emit;
using System.Reflection;

namespace CK.SqlServer.Setup
{
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = false )]
    public class SqlProcedureAttribute : SqlMethodForObjectItemAttributeBase
    {
        public SqlProcedureAttribute( string procedureName )
            : base( procedureName, "CK.SqlServer.Setup.SqlProcedureAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
        }
    }
}
