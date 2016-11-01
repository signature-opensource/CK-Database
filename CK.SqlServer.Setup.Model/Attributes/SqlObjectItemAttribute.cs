using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using CK.Setup;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Declares a resource that contains a Sql procedure, function or view associated to a type.
    /// Multiples object names like "sUserCreate, sUserDestroy, AnotherSchema.sUserUpgrade, CK.sUserRun" can be defined.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlObjectItemAttribute : SetupObjectItemAttributeBase
    {
        /// <summary>
        /// Initializes a new <see cref="SqlObjectItemAttribute"/> with (potentially) multiple object names.
        /// </summary>
        /// <param name="commaSeparatedObjectNames">Name or multiple comma separated names.</param>
        public SqlObjectItemAttribute( string commaSeparatedObjectNames )
            : this( commaSeparatedObjectNames, "CK.SqlServer.Setup.SqlObjectItemAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SqlObjectItemAttribute"/> with (potentially) multiple object names.
        /// </summary>
        /// <param name="commaSeparatedObjectNames">Name or multiple comma separated names.</param>
        /// <param name="actualAttributeTypeAssemblyQualifiedName">
        /// Assembly Qualified Name of the object that will replace this attribute during setup.
        /// </param>
        protected SqlObjectItemAttribute( string commaSeparatedObjectNames, string actualAttributeTypeAssemblyQualifiedName )
            : base( commaSeparatedObjectNames, actualAttributeTypeAssemblyQualifiedName )
        {
            MissingDependencyIsError = true;
        }

        /// <summary>
        /// Gets or sets whether when installing, the informational message 'The module 'X' depends 
        /// on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
        /// must be logged as a <see cref="LogLevel.Error"/>. When false, this is a <see cref="LogLevel.Info"/>.
        /// Defaults to true.
        /// </summary>
        public bool MissingDependencyIsError { get; set; }

    }
}
