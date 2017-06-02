#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\AmbientContract\Attribute\IAttributeAmbientContextBound.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Marker interface for attributes so that they are bound to an Ambient type. Attributes instances are 
    /// cached: their lifecycle are then the same as the contextualized type information.
    /// </summary>
    public interface IAttributeAmbientContextBound
    {
    }
    
}
