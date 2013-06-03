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
    public class SqlProcedureAttribute : AmbientContextBoundDelegationAttribute
    {
        public SqlProcedureAttribute( string procedureName )
            : base( "CK.SqlServer.Setup.SqlProcedureAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            ProcedureName = procedureName;
            MissingDependencyIsError = true;
        }

        /// <summary>
        /// Gets or sets the procedure name.
        /// </summary>
        public string ProcedureName { get; private set; }

        /// <summary>
        /// Gets or sets whether when installing, the informational message 'The module 'X' depends 
        /// on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
        /// must be logged as a <see cref="LogLevel.Error"/>. When false, this is a <see cref="LogLevel.Info"/>.
        /// Defaults to true.
        /// </summary>
        public bool MissingDependencyIsError { get; set; }

    }
}
