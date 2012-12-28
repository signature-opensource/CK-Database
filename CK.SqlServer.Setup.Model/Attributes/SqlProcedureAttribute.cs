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
    public class SqlProcedureAttribute : AmbientContextBoundDelegationAttribute, IAttributeAutoImplemented
    {
        MethodInfo _method;

        public SqlProcedureAttribute( string procedureName )
            : base( "CK.SqlServer.Setup.SqlProcedureAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            ProcedureName = procedureName;
        }

        public string ProcedureName { get; private set; }

    }
}
