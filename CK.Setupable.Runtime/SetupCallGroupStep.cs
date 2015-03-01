#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\SetupCallGroupStep.cs) is part of CK-Database. 
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
    /// Extends the <see cref="SetupStep"/> to support the "Content" of a Group.
    /// </summary>
    public enum SetupCallGroupStep
    {
        None = 0,
        Init            = 1,
        InitContent     = 2,
        Install         = 3,
        InstallContent  = 4,
        Settle          = 5,
        SettleContent   = 6
    }
}
