#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\ISetupScript.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// A <see cref="ParsedFileName"/> associated to a way to read its content and a script source name.
    /// </summary>
    public interface ISetupScript
    {
        /// <summary>
        /// Gets the name of this script.
        /// </summary>
        ParsedFileName Name { get; }

        /// <summary>
        /// Gets the source name.
        /// Never be null nor empty.
        /// </summary>
        string ScriptSource { get; }

        /// <summary>
        /// Gets the script itself.
        /// </summary>
        /// <returns>The script text.</returns>
        string GetScript();
    }
}
