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
    public class SqlProcedureNoExecuteAttribute : SqlCallableAttributeBase
    {
        public SqlProcedureNoExecuteAttribute( string procedureName )
            : base( procedureName, "Procedure" )
        {
        }
        
    }

}
