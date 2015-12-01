#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\SetupScriptExtension.cs) is part of CK-Database. 
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
    /// Contains extension methods for <see cref="ISetupScript"/>.
    /// </summary>
    public static class SetupScriptExtension
    {
        /// <summary>
        /// Computes a key that identifies a <see cref="ISetupScript"/>: it is a combination
        /// of the <see cref="ISetupScript.ScriptSource"/>, <see cref="ParsedFileName.FullName"/>, <see cref="ParsedFileName.CallContainerStep"/>, 
        /// <see cref="ParsedFileName.FromVersion"/> and <see cref="ParsedFileName.Version"/>.
        /// </summary>
        /// <param name="this">This <see cref="ISetupScript"/> object.</param>
        /// <param name="suffix">Optional suffix to append to the key (avoids another concatenation).</param>
        /// <returns>A key that identifies this script: two scripts with this same key can not both participate in a setup.</returns>
        public static string GetScriptKey( this ISetupScript @this, string suffix = null )
        {
            return String.Format( "{0}|{1}|{2}|{3}|{4}|{5}", @this.ScriptSource, @this.Name.FullName, @this.Name.CallContainerStep, @this.Name.FromVersion, @this.Name.Version, suffix ); 
        }

    }
}
