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
    public abstract class SqlMethodForObjectItemAttributeBase : AmbientContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes this attribute this a name (procedure name like "sUserCreate")
        /// </summary>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
        protected SqlMethodForObjectItemAttributeBase( string objectName, string actualAttributeTypeAssemblyQualifiedName )
            : base( actualAttributeTypeAssemblyQualifiedName )
        {
            ObjectName = objectName;
            MissingDependencyIsError = true;
        }

        /// <summary>
        /// Gets the object name (for instance "sUserCreate").
        /// </summary>
        public string ObjectName { get; private set; }

        /// <summary>
        /// Gets or sets whether when installing, the informational message 'The module 'X' depends 
        /// on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
        /// must be logged as a <see cref="LogLevel.Error"/>. When false (that is the default), this is a <see cref="LogLevel.Info"/>.
        /// </summary>
        public bool MissingDependencyIsError { get; set; }

    }
}
