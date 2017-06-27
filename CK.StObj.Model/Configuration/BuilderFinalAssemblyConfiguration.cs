using System;
using System.Xml.Linq;

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

        public BuilderFinalAssemblyConfiguration()
        {
        }

        static readonly XName xDirectory = XNamespace.None + "Directory";
        static readonly XName xAssemblyName = XNamespace.None + "AssemblyName";
        static readonly XName xSourceGeneration = XNamespace.None + "SourceGeneration";
        static readonly XName xSignAssembly = XNamespace.None + "SignAssembly";

        /// <summary>
        /// Initializes a new <see cref="BuilderFinalAssemblyConfiguration"/> from a <see cref="XElement"/>.
        /// </summary>
        /// <param name="e">The xml element.</param>
        public BuilderFinalAssemblyConfiguration( XElement e, int currentXmlVersion )
        {
            Directory = e.Element( xDirectory )?.Value;
            AssemblyName = e.Element( xAssemblyName )?.Value;
            SourceGeneration = string.Equals( e.Element( xSourceGeneration )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            SignAssembly = string.Equals( e.Element( xSignAssembly )?.Value, "true", StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// The <see cref="AssemblyRegistererConfiguration(XElement)"/> constructor will be able to read this element back.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( string.IsNullOrWhiteSpace( Directory ) ? null : new XElement( xDirectory, Directory ),
                   string.IsNullOrWhiteSpace( AssemblyName ) ? null : new XElement( xAssemblyName, AssemblyName ),
                   SourceGeneration ? new XElement( xSourceGeneration, "true" ) : null,
                   SignAssembly ? new XElement( xSignAssembly, "true" ) : null );
            return e;
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
        public bool SourceGeneration { get; set; }
#else
        /// <summary>
        /// Always true since on .Net core this is the only way...
        /// </summary>
        public bool SourceGeneration { get => true; set { } }
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
