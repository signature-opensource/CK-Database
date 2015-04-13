#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\RequiredByAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Simple attributes to define reverted requirements of a class by names.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RequiredByAttribute : RequiresAttribute
    {
        /// <summary>
        /// Defines reverse requirements by their names.
        /// </summary>
        /// <param name="requires">Comma separated list of item names that require this object.</param>
        public RequiredByAttribute( string requires )
            : base( requires )
        {            
        }

    }
}
