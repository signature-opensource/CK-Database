using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.SqlServer
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class SqlPackageAttribute : PackageAttribute
    {
        public SqlPackageAttribute( string fullName, string versions )
            : base( fullName, versions )
        {
            DefaultDatabase = typeof( SqlDefaultDatabase );
        }

        /// <summary>
        /// Gets or sets the default database associated to this package.
        /// It must be a <see cref="SqlDatabase"/> type and defaults to the type of <see cref="SqlDefaultDatabase"/>.
        /// </summary>
        public Type DefaultDatabase { get; set; }

    }
}
