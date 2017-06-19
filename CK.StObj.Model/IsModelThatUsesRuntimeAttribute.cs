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
        /// Initializes a new setup runtime attribute that declares a required runtime
        /// (this must be declared on a Model assembly).
        /// </summary>
        /// <param name="assemblyName">Name of the runtime assembly.</param>
        /// <param name="version">Optional version.</param>
        public IsModelThatUsesRuntimeAttribute( string assemblyName, string version = null )
        {
            AssemblyName = assemblyName;
            Version = version;
        }

        /// <summary>
        /// Gets the name of the runtime assembly.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Gets an optional version for the runtime assembly.
        /// </summary>
        public string Version { get; }
    }
}
