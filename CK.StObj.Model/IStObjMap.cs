#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\IStObjMap.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Main interface that offers access to type mapping and Ambient Object instances.
    /// </summary>
    public interface IStObjMap
    {
        /// <summary>
        /// Gets the StObjs map.
        /// </summary>
        IStObjObjectMap StObjs { get; }

        /// <summary>
        /// Gets the Services map.
        /// </summary>
        IStObjServiceMap Services { get; }

        /// <summary>
        /// Gets the name of this StObj map.
        /// Never null, defaults to the empty string.
        /// </summary>
        string MapName { get; }
    }
}
