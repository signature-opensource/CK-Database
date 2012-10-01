using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlPackageBase 
    {
        /// <summary>
        /// Gets or sets the database to which this package belongs.
        /// Typically initialized by an attribute (like <see cref="SqlPackageAttribute"/>) when specialized as a StObj.
        /// </summary>
        [AmbientProperty]
        public SqlDatabase Database { get; set; }

        /// <summary>
        /// Gets or sets the sql schema.
        /// Typically initialized by an attribute (like <see cref="SqlPackageAttribute"/>) when specialized as a StObj.
        /// </summary>
        [AmbientProperty]
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="ResourceLocation"/> that locates the resources associated 
        /// to this package.
        /// Typically initialized by an attribute (like <see cref="SqlPackageAttribute"/>) when specialized as a StObj.
        /// </summary>
        [AmbientProperty]
        public ResourceLocator ResourceLocation { get; set; }

    }
}
