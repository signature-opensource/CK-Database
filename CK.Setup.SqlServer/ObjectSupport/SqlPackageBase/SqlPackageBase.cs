using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    [StObj( ItemKind = DependentItemKind.Container )]
    [StObjProperty( PropertyName = "ResourceLocation", PropertyType = typeof(ResourceLocator) ) ]
    public class SqlPackageBase 
    {
        /// <summary>
        /// Gets or sets the database to which this package belongs.
        /// Typically initialized by an attribute (like <see cref="SqlPackageAttribute"/>).
        /// </summary>
        [AmbientProperty]
        public SqlDatabase Database { get; set; }

        /// <summary>
        /// Gets or sets the sql schema.
        /// Typically initialized by an attribute (like <see cref="SqlPackageAttribute"/>).
        /// </summary>
        [AmbientProperty]
        public string Schema { get; set; }

    }
}
