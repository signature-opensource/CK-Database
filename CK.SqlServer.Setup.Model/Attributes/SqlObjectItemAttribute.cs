using System;
using CK.Setup;

namespace CK.Core
{

    /// <summary>
    /// Declares a resource that contains a Sql procedure, function or view.
    /// Multiples object names like "sUserCreate, sUserDestroy, AnotherSchema.sUserUpgrade, CK.vProduct" can be defined.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlObjectItemAttribute : SetupObjectItemAttributeBase
    {
        /// <summary>
        /// Initializes a new <see cref="SqlObjectItemAttribute"/> with (potentially) multiple object names.
        /// </summary>
        /// <param name="commaSeparatedObjectNames">Name or multiple comma separated names.</param>
        public SqlObjectItemAttribute( string commaSeparatedObjectNames )
            : this( commaSeparatedObjectNames, "CK.SqlServer.Setup.SqlBaseItemAttributeImpl, CK.SqlServer.Setup.Runtime" )
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
        }

    }
}
