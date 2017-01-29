#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\AmbientContract\IAmbientContract.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface marker states that a class or an interface instance
    /// must be unique in a context. 
    /// </summary>
    /// <remarks>
    /// The notion of "context" is not defined at this level, this interface 
    /// only declares the type as beeing a "pseudo singleton" for a scope that can be global or contextualized. 
    /// </remarks>
    public interface IAmbientContract
    {
    }

}
