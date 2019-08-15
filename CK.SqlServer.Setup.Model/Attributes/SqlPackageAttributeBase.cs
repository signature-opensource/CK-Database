using System;
using CK.Core;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Base attribute for <see cref="SqlTableAttribute"/> and <see cref="SqlPackageAttribute"/>.
    /// </summary>
    public abstract class SqlPackageAttributeBase : AmbientContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="SqlPackageAttributeBase"/>.
        /// </summary>
        /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
        protected SqlPackageAttributeBase( string actualAttributeTypeAssemblyQualifiedName )
            : base( actualAttributeTypeAssemblyQualifiedName )
        {
        }

        /// <summary>
        /// Gets or sets the package to which this package belongs.
        /// </summary>
        public Type Package { get; set; }

        /// <summary>
        /// Gets or sets the sql schema to use.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the Resource path to use for the <see cref="IResourceLocator"/>. 
        /// </summary>
        public string ResourcePath { get; set; }

        /// <summary>
        /// Gets or sets the Resource Type to use for the <see cref="IResourceLocator"/>.
        /// When null (the default that should rarely be changed), it is the decorated type itself that is 
        /// used to locate the resources.
        /// </summary>
        public Type ResourceType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="SqlDatabase"/> type targeted by the package. Let it to null to use the ambient one.
        /// The <see cref="SqlPackage.Database"/> property is automatically set (see remarks).
        /// </summary>
        /// <remarks>
        /// The type must be a specialization of <see cref="SqlDatabase"/>. 
        /// If it supports <see cref="IAmbientObject"/>, the property is bound to the corresponding ambient contract instance. 
        /// </remarks>
        public Type Database { get; set; }
    }
}
