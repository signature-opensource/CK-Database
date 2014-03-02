using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    [StObjProperty( PropertyName = "ResourceLocation", PropertyType = typeof( ResourceLocator ) )]
    public class SqlSetupableBase
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

    [StObj( ItemKind = DependentItemKindSpec.Container )]
    public class SqlPackageBase : SqlSetupableBase
    {
        /// <summary>
        /// Gets or sets whether this package is associated to a Model.
        /// Defaults to false.
        /// </summary>
        public bool HasModel { get; set; }
    }
}
