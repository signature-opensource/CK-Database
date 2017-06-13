using System;
using System.Collections.Generic;
using System.Text;


namespace CK.Setup
{

    /// <summary>
    /// Decorates an assembly with an associated setup dependency. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true )]
    public class SetupDependencyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new setup dependency attribute.
        /// </summary>
        /// <param name="assemblyName">Name of the companion assembly.</param>
        /// <param name="version">Optional version.</param>
        public SetupDependencyAttribute( string assemblyName, string version = null )
        {
            AssemblyName = assemblyName;
            Version = version;
        }

        /// <summary>
        /// Gets the name of the companion assembly.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Gets an optional version for the companion assembly.
        /// </summary>
        public string Version { get; }
    }
}
