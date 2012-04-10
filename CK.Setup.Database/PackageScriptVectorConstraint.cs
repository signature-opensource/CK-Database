using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.Database
{
    /// <summary>
    /// Defines the constraints that a <see cref="PackageScriptVector"/> may satisfy.
    /// </summary>
    [Flags]
    public enum PackageScriptVectortConstraint
    {
        None = 0,

        /// <summary>
        /// The "no version" script must exist ('no version" script is always applied last).
        /// </summary>
        NoVersionIsRequired = 1,

        /// <summary>
        /// A script for the current version is required.
        /// </summary>
        CurrentVersionIsRequired = 2,

        /// <summary>
        /// The migration path must have no holes.
        /// </summary>
        UpgradeVersionPathIsComplete = 4
    }
}
