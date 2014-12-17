#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Runtime\IStObjTrackedAmbientPropertyInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Setup
{
    public interface IStObjTrackedAmbientPropertyInfo
    {
        /// <summary>
        /// Gets the <see cref="IStObjResult"/> that holds the property.
        /// </summary>
        IStObjResult Owner { get; }

        /// <summary>
        /// Gets the property information.
        /// </summary>
        PropertyInfo PropertyInfo { get; }
    }
}
