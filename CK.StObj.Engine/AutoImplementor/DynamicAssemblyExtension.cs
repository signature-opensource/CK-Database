#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AutoImplementor\DynamicAssembly.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections;

namespace CK.Core
{
    static class DynamicAssemblyExtension
    {
        /// <summary>
        /// Gets a type name with a "&lt;CK&gt;" namespace and a <see cref="NextUniqueNumber"/> suffix
        /// or a guid when the <paramref name="baseName"/> is null.
        /// </summary>
        /// <param name="name">Base type name.</param>
        /// <returns>A unique type name.</returns>
        public static string AutoNextTypeName( this IDynamicAssembly @this, string name = null )
        {
            return "<CK>." + (name != null ? name + @this.NextUniqueNumber() : Guid.NewGuid().ToString());
        }
    }
}
