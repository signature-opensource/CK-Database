using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// A <see cref="ParsedFileName"/> associated to a way to 
    /// read its content and a script type.
    /// </summary>
    public static class SetupScriptExtension
    {
        /// <summary>
        /// Computes a key that identifies a <see cref="ISetupScript"/>: it is a combination
        /// of the <see cref="ISetupScript.ScriptType"/>, <see cref="ParsedFileName.ContainerFullName"/>, <see cref="ParsedFileName.CallContainerStep"/>, 
        /// <see cref="ParsedFileName.FromVersion"/> and <see cref="ParsedFileName.Version"/>.
        /// </summary>
        /// <param name="this">This <see cref="ISetupScript"/> object.</param>
        /// <param name="suffix">Optional suffix to append to the key (avoids another concatenation).</param>
        /// <returns>A key that identifies this script: two scripts with this same key can not both participate in a setup.</returns>
        public static string GetScriptKey( this ISetupScript @this, string suffix = null )
        {
            return String.Format( "{0}|{1}|{2}|{3}|{4}|{5}", @this.ScriptType, @this.Name.ContainerFullName, @this.Name.CallContainerStep, @this.Name.FromVersion, @this.Name.Version, suffix ); 
        }

    }
}
