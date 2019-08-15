using System;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Attribute that must decorate a <see cref="SqlPackage"/> class.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlPackageAttribute : SqlPackageAttributeBase, IAttributeSetupName
    {
        /// <summary>
        /// Initializes a new <see cref="SqlPackageAttribute"/>.
        /// </summary>
        public SqlPackageAttribute()
            : base( "CK.SqlServer.Setup.SqlPackageAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            HasModel = true;
        }

        /// <summary>
        /// Gets or sets whether this package has an associated Model.
        /// Defaults to true.
        /// It can be set to false only for packages that do not contain any model package.
        /// </summary>
        public bool HasModel { get; set; }

        /// <summary>
        /// Gets or sets the full name (for the setup process).
        /// Defaults to the <see cref="Type.FullName"/> of the decorated package type.
        /// </summary>
        public string FullName { get; set; }
    
    }
}
