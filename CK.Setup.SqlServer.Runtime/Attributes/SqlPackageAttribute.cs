using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlPackageAttribute : SqlPackageAttributeBase, IAttributeSetupName
    {
        public SqlPackageAttribute()
            : base( "CK.Setup.SqlServer.SqlPackageAttributeImpl, CK.Setup.SqlServer" )
        {
        }

        /// <summary>
        /// Gets or sets whether this package has an associated Model.
        /// Defaults to false.
        /// </summary>
        public bool HasModel { get; set; }

        /// <summary>
        /// Gets or sets the full name (for the setup process).
        /// Defaults to the <see cref="Type.Name"/> of the decorated package type.
        /// </summary>
        public string FullName { get; set; }
    
    }
}
