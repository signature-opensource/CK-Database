#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\AutoImplementor\PreventAutoImplementationAttribute.cs) is part of CK-Database. 
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
    /// When applied on an abstract class, prevents any kind of auto implementation.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class PreventAutoImplementationAttribute : Attribute
    {
    }
}
