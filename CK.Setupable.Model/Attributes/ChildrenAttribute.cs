#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\ChildrenAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Setup
{
    /// <summary>
    /// Simple attributes to define children of a class by names.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class ChildrenAttribute : RequiresAttribute
    {
        /// <summary>
        /// Defines children by their names.
        /// </summary>
        /// <param name="children">Comma separated list of children names.</param>
        public ChildrenAttribute( string children )
            : base( children )
        {            
        }

    }
}
