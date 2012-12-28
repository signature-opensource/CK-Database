using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Setup
{
    public class SqlPackage : SqlPackageBase, IAmbientContractDefiner
    {
        /// <summary>
        /// Gets or sets whether this package is associated to a Model.
        /// Defaults to false.
        /// </summary>
        public bool HasModel { get; set; }
    }
}
