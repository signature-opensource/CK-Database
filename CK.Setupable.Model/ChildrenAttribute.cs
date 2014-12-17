#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\ChildrenAttribute.cs) is part of CK-Database. 
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
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class ChildrenAttribute : RequiresAttribute
    {
        /// <summary>
        /// Defines children by their names.
        /// </summary>
        /// <param name="requires">Comma separated list of children names.</param>
        public ChildrenAttribute( string children )
            : base( children )
        {            
        }

    }
}
