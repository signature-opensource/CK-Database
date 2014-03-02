using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Setup
{
    public abstract class SqlSetupableAttributeBase : AmbientContextBoundDelegationAttribute
    {
        protected SqlSetupableAttributeBase( string actualAttributeTypeAssemblyQualifiedName )
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
        /// Gets or sets the Resource path to use for the <see cref="ResourceLocator"/>. 
        /// </summary>
        public string ResourcePath { get; set; }

        /// <summary>
        /// Gets or sets the Resource Type to use for the <see cref="ResourceLocator"/>.
        /// When null (the default that should rarely be changed), the decorated type is used: resources 
        /// must be in its assembly.
        /// </summary>
        public Type ResourceType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="SqlDatabase"/> type targeted by the package. Let it to null to use the ambient one.
        /// The <see cref="SqlPackage.Database"/> property is automatically set (see remarks).
        /// </summary>
        /// <remarks>
        /// The type must be a specialization of <see cref="SqlDatabase"/>. 
        /// If it supports <see cref="IAmbientContract"/>, the property is bound to the corresponding ambient contract instance. 
        /// </remarks>
        public Type Database { get; set; }
    }

    public abstract class SqlPackageAttributeBase : SqlSetupableAttributeBase
    {
        protected SqlPackageAttributeBase( string actualAttributeTypeAssemblyQualifiedName )
            : base( actualAttributeTypeAssemblyQualifiedName )
        {
        }

    }
}
