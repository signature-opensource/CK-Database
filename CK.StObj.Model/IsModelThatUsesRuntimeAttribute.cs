using System;
using System.Collections.Generic;
using System.Text;


namespace CK.Setup
{

    /// <summary>
    /// Decorates an assembly with an associated runtime assembly. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true )]
    public class IsModelThatUsesRuntimeAttribute : Attribute
    {
        /// <summary>
        /// Default <see cref="MinRuntimeversion"/> value is to consider that the Runtime version
        /// is synchronized with the Model version.
        /// </summary>
        public const string MinRuntimeVersionIsModelVersion = "UseModelVersion";

        /// <summary>
        /// Initializes a new setup runtime attribute that declares a required runtime
        /// (this must be declared on a Model assembly).
        /// </summary>
        /// <param name="assemblyName">Name of the runtime assembly.</param>
        /// <param name="minRuntimeversion">
        /// Optional version. By default, the Runtime must have at least the version of the Model.
        /// Setting it to null removes all version constraints and setting it to a specific version
        /// states that subsequent version of the Runtime should continue to be able to handle this Model.
        /// </param>
        public IsModelThatUsesRuntimeAttribute( string assemblyName, string minRuntimeversion = MinRuntimeVersionIsModelVersion )
        {
            AssemblyName = assemblyName;
            MinRuntimeversion = minRuntimeversion;
        }

        /// <summary>
        /// Gets the name of the runtime assembly.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Gets an optional version for the runtime assembly.
        /// Optional version. By default, the Runtime must have at least the version of the Model
        /// (via the special string <see cref="MinRuntimeVersionIsModelVersion"/>).
        /// Setting it to null removes all version constraints and setting it to a specific version
        /// states that subsequent version of the Runtime should continue to be able to handle this Model.
        /// </summary>
        public string MinRuntimeversion { get; }
    }
}
