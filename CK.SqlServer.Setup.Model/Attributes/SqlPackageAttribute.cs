using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlPackageAttribute : SqlPackageAttributeBase, IAttributeSetupName
    {
        public SqlPackageAttribute()
            : base( "CK.SqlServer.Setup.SqlPackageAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            HasModel = true;
        }

        /// <summary>
        /// Gets or sets whether this package has an associated Model.
        /// Defaults to true.
        /// It should be set to false only for packages that do not contain any model package.
        /// </summary>
        public bool HasModel { get; set; }

        /// <summary>
        /// Gets or sets the full name (for the setup process).
        /// Defaults to the <see cref="Type.FullName"/> of the decorated package type.
        /// </summary>
        public string FullName { get; set; }
    
    }
}
