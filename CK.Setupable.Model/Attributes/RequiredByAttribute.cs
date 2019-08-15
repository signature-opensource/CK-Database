#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\RequiredByAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Core
{
    /// <summary>
    /// Simple attributes to define reverted requirements of a class by names.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RequiredByAttribute : Setup.BaseItemNamesAttribute
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
