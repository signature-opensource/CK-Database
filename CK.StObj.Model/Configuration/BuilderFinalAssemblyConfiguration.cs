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
        /// When null (the default) the <see cref="AppContext.BaseDirectory"/> is used.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Gets or sets the final Assembly name.
        /// When null (the default), <see cref="DefaultAssemblyName"/> "CK.StObj.AutoAssembly" is used.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// True to sign the final assembly.
        /// </summary>
        public bool SignAssembly { get; set; }

#if NET461
        /// <summary>
        /// Gets or sets whether source code generation is also done.
        /// </summary>
        public bool TemporaryGenerateSrc { get; set; }
#else
        /// <summary>
        /// Always true since on .Net core this is the only way...
        /// </summary>
        public bool TemporaryGenerateSrc { get => true; set { } }
#endif
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
