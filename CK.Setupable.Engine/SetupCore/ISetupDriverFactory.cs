#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\SetupCore\ISetupDriverFactory.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ISetupDriverFactory
    {
        /// <summary>
        /// Creates a (potentially configured) instance of <see cref="SetupItemDriver"/> of a given <paramref name="driverType"/>.
        /// </summary>
        /// <param name="driverType">SetupDriver type to create.</param>
        /// <param name="info">Internal constructor information.</param>
        /// <returns>A setup driver. Null if not able to create it (a basic <see cref="Activator.CreateInstance(Type)"/> will be used to create the driver).</returns>
        SetupItemDriver CreateDriver( Type driverType, SetupItemDriver.BuildInfo info );
    }
}
