using System;
using System.Collections.Generic;
using System.Text;


namespace CK.Setup
{
    /// <summary>
    /// Decorates a runtime assembly with an associated engine assembly. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true )]
    public class IsRuntimeThatUsesEngineAttribute : Attribute
    {
        /// <summary>
        /// Default <see cref="MinEngineVersion"/> value is to consider that the Engine version
        /// is synchronized with the Runtime version.
        /// </summary>
        public const string MinEngineVersionIsRuntimeVersion = "UseRuntimeVersion";

        /// <summary>
        /// Initializes a new setup engine attribute that declares a required engine
        /// (this must be declared on a runtime assembly).
        /// </summary>
        /// <param name="assemblyName">Name of the engine assembly.</param>
        /// <param name="minEngineversion">
        /// Optional version. By default, the Runtime must have at least the version of the Model.
        /// Setting it to null removes all version constraints and setting it to a specific version
        /// states that subsequent version of the Runtime should continue to be able to handle this Model.
        /// </param>
        public IsRuntimeThatUsesEngineAttribute( string assemblyName, string minEngineversion = MinEngineVersionIsRuntimeVersion )
        {
            AssemblyName = assemblyName;
            MinEngineVersion = minEngineversion;
        }

        /// <summary>
        /// Gets the name of the engine assembly.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Gets the minimal version for the engine assembly.
        /// When null, it removes all version constraints.
        /// The special value "UseRuntimeVersion" can be used when the Runtime is the default, 
        /// primary one of the Engine and that both projects share the same repository.
        /// </summary>
        public string MinEngineVersion { get; }
    }
}
