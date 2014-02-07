using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace CK.Core
{
    /// <summary>
    /// Defines options related to final assembly generation.
    /// </summary>
    [Serializable]
    public class BuilderFinalAssemblyConfiguration
    {
        /// <summary>
        /// Default assembly name.
        /// </summary>
        public const string DefaultAssemblyName = "CK.StObj.AutoAssembly";

        /// <summary>
        /// Flags that prevents final assembly generation.
        /// </summary>
        public bool DoNotGenerateFinalAssembly { get; set; }

        /// <summary>
        /// Gets or set the directory where the final assembly must be saved.
        /// When null (the default), the local path of the CodeBase of CK.StObj.Model assembly is used.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Gets or sets the final Assembly name.
        /// When null (the default), <see cref="DefaultAssemblyName"/> "CK.StObj.AutoAssembly" is used.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Gets or sets a string (that can have any value) that will be stored inside the final assembly.
        /// The <see cref="StObjContextRoot.LoadOrBuild"/> method use this key to automatically trigger a build 
        /// and a new generation of the final assembly if the string do not match.
        /// </summary>
        public string ExternalVersionStamp { get; set; }

        /// <summary>
        /// True to sign the final assembly. See <see cref="SignKeyPair"/>.
        /// </summary>
        public bool SignAssembly { get; set; }

        /// <summary>
        /// <see cref="StrongNameKeyPair"/> used to sign the assembly. 
        /// Must be null if <see cref="SignAssembly"/> is false, but can be let to null when SignAssembly is true:
        /// an embedded key pair is automatically used with a public key of:
        /// "00240000048000009400000006020000002400005253413100040000010001009fbf2868f04bdf33df4c8c0517bb4c3d743b5b27fcd94009d42d6607446c1887a837e66545221788ecfff8786e85564c839ff56267fe1a3225cd9d8d9caa5aae3ba5d8f67f86ff9dbc5d66f16ba95bacde6d0e02f452fae20022edaea26d31e52870358d0dda69e592ea5cef609a054dac4dbbaa02edc32fb7652df9c0e8e9cd"
        /// </summary>
        public StrongNameKeyPair SignKeyPair { get; set; }

        /// <summary>
        /// Uses <see paramref="directory"/> if it is not null nor empty, otherwise tries to use the path of CK.StObj.Model assembly.
        /// </summary>
        /// <returns>The directory into which the final assembly must be saved.</returns>
        static public string GetFinalDirectory( string directory )
        {
            return String.IsNullOrEmpty( directory ) ? Path.GetDirectoryName( new Uri( typeof( StObjContextRoot ).Assembly.CodeBase ).LocalPath ) : directory;
        }
        
        /// <summary>
        /// Uses <paramref cref="assemblyName"/> if it is not null nor empty or <see cref="DefaultAssemblyName"/>.
        /// </summary>
        /// <returns>Final assembly name.</returns>
        static public string GetFinalAssemblyName( string assemblyName )
        {
            return String.IsNullOrEmpty( assemblyName ) ? DefaultAssemblyName : assemblyName;
        }
    }
}
