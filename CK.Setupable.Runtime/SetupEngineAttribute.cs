using System;
using System.Collections.Generic;
using System.Text;


namespace CK.Setup
{

    /// <summary>
    /// Decorates an assembly with an associated engine assembly. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true )]
    public class SetupEngineAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new setup engine attribute that declares a required engine
        /// (this must be declared on a runtime assembly).
        /// </summary>
        /// <param name="assemblyName">Name of the engine assembly.</param>
        /// <param name="version">Optional version.</param>
        public SetupEngineAttribute( string assemblyName, string version = null )
        {
            AssemblyName = assemblyName;
            Version = version;
        }

        /// <summary>
        /// Gets the name of the engine assembly.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Gets an optional version for the engine assembly.
        /// </summary>
        public string Version { get; }
    }
}
