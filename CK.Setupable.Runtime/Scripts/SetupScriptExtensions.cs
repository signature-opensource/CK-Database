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
    public static class SetupScriptExtensions
    {
        /// <summary>
        /// Computes a key that identifies a <see cref="ISetupScript"/>: it is a combination
        /// of the <see cref="ISetupScript.Extension"/>, <see cref="ParsedFileName.FullName"/>, 
        /// <see cref="ParsedFileName.CallContainerStep"/>, <see cref="ParsedFileName.FromVersion"/> 
        /// and <see cref="ParsedFileName.Version"/>.
        /// </summary>
        /// <param name="this">This <see cref="ISetupScript"/> object.</param>
        /// <param name="suffix">Optional suffix to append to the key (avoids another concatenation).</param>
        /// <returns>A key that identifies this script: two scripts with this same key can not both participate in a setup.</returns>
        public static string GetScriptKey( this ISetupScript @this, string suffix = null )
        {
            return $"{@this.Name.Extension}|{@this.Name.FullName}|{@this.Name.CallContainerStep}|{@this.Name.FromVersion}|{@this.Name.Version}|{suffix}"; 
        }

        /// <summary>
        /// Creates a new <see cref="ISetupScript"/> with a new script and extension.
        /// </summary>
        /// <param name="this">This setup script.</param>
        /// <param name="newScript">The new script text.</param>
        /// <param name="newExtension">The new extension.</param>
        /// <returns>A new setup script object.</returns>
        public static ISetupScript SetScriptAndExtension( this ISetupScript @this, string newScript, string newExtension )
        {
            ParsedFileName newName = @this.Name.SetExtension( newExtension );
            return new SetupScript( newName, newScript );
        }

    }
}
