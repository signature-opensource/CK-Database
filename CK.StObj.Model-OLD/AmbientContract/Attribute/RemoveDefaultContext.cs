#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\AmbientContract\Attribute\RemoveDefaultContext.cs) is part of CK-Database. 
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
    /// Attribute that removes an object from the default context (identified by the empty string). 
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RemoveDefaultContextAttribute : RemoveContextAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="RemoveDefaultContextAttribute"/>.
        /// </summary>
        public RemoveDefaultContextAttribute()
            : base( String.Empty )
        {
        }
    }
}
