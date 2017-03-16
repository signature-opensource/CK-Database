using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace CK.Core
{

    public class OtherPlatformSupportConfiguration
    {
        /// <summary>
        /// Gets or sets the folder where the assemblies that target another
        /// framework resides. Must not be null.
        /// </summary>
        public string BinFolder { get; set; }

        /// <summary>
        /// Gets the list of assembly names for which calls must be redirected.
        /// </summary>
        public IList<string> AssemblyNamesToRedirect { get; } = new List<string>();

        /// <summary>
        /// Gets the list of assembly names that must be removed from the transformed assembly.
        /// </summary>
        public IList<string> AssemblyNamesToRemove { get; } = new List<string>();
    }

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
        /// Describes how and if the generated assembly should be saved to disk.
        /// </summary>
        public enum GenerateOption
        {
            /// <summary>
            /// Saves the generated assembly.
            /// </summary>
            GenerateFile = 0,

            /// <summary>
            /// Default is <see cref="GenerateFile"/>.
            /// </summary>
            Default = GenerateFile,

            /// <summary>
            /// Does not save the generated assembly file.
            /// </summary>
            DoNotGenerateFile = 1,

            /// <summary>
            /// Saves the generated assembly and calls PEVerify on it.
            /// </summary>
            GenerateFileAndPEVerify = 2
        }


        /// <summary>
        /// Options that may prevent final assembly generation: the final asembly is always
        /// created to be able to interact with eventually implemented StObj, this option can 
        /// prevents the assembly to be saved on disk or saving it and verifying it through PEVerify.
        /// </summary>
        public GenerateOption GenerateFinalAssemblyOption { get; set; }

        /// <summary>
        /// Gets or set the directory where the final assembly must be saved.
        /// When null (the default) the current path of CK.StObj.Model assembly is used
        /// (thanks to <see cref="GetFinalDirectory(string)"/>).
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Gets or sets the final Assembly name.
        /// When null (the default), <see cref="DefaultAssemblyName"/> "CK.StObj.AutoAssembly" is used.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Gets the full path of the generated assembly (that may not exist) based on <see cref="Directory"/>, 
        /// <see cref="AssemblyName"/> and <see cref="GetFinalDirectory"/> and <see cref="GetFinalAssemblyName"/>
        /// helpers. The returned path ends with a ".dll".
        /// </summary>
        public string GeneratedAssemblyPath
        {
            get
            {
                return Path.Combine( GetFinalDirectory( Directory ), GetFinalAssemblyName( AssemblyName ) + ".dll" );
            }
        }

        /// <summary>
        /// Gets or sets a string (that can have any value) that will be stored inside the final assembly.
        /// The <see cref="G:StObjContextRoot.Build"/> method use this key to automatically trigger a build 
        /// and a new generation of the final assembly if the string do not match.
        /// </summary>
        public string ExternalVersionStamp { get; set; }

        /// <summary>
        /// True to sign the final assembly.
        /// </summary>
        public bool SignAssembly { get; set; }

        /// <summary>
        /// Uses <see paramref="directory"/> if it is not null nor empty, otherwise 
        /// uses the directory where CK.StObj.Model.dll is.
        /// </summary>
        /// <returns>The directory into which the final assembly must be saved.</returns>
        static public string GetFinalDirectory( string directory )
        {
            if( string.IsNullOrEmpty( directory ) )
            {
                // netstandard1.6 handles this:
                directory = Path.GetDirectoryName( new Uri( typeof( StObjContextRoot ).GetTypeInfo().Assembly.CodeBase ).LocalPath );
            }
            return directory;
        }

        /// <summary>
        /// Uses <paramref name="assemblyName"/> if it is not null nor empty or <see cref="DefaultAssemblyName"/>.
        /// </summary>
        /// <returns>Final assembly name.</returns>
        static public string GetFinalAssemblyName( string assemblyName )
        {
            return string.IsNullOrEmpty( assemblyName ) ? DefaultAssemblyName : assemblyName;
        }
    }
}
