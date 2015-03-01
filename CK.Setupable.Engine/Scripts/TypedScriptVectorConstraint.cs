#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\TypedScriptVectorConstraint.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Defines the constraints that a <see cref="TypedScriptVector"/> may satisfy.
    /// </summary>
    [Flags]
    public enum TypedScriptVectortConstraint
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
