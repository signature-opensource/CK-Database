#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\IAttributeSetupName.cs) is part of CK-Database. 
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
    /// Enables any attributes other than <see cref="SetupAttribute"/> and <see cref="SetupName"/> 
    /// to carry the full name of a setup object.
    /// </summary>
    public interface IAttributeSetupName
    {
        /// <summary>
        /// Gets the full name of the setup object.
        /// </summary>
        string FullName { get; }
    }
}
