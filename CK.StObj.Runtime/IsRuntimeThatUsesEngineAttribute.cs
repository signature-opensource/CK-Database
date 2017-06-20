using System;
using System.Collections.Generic;
using System.Text;


namespace CK.Setup
{
    /// <summary>
    /// Decorates an assembly with an associated engine assembly. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true )]
    public class IsRuntimeThatUsesEngineAttribute : Attribute
    {
        /// <summary>
        /// Special value that forces the Engine version to be synchronized with the Model version.
        /// This is not the default since it applies only to the primary Runtime of an Engine.
        /// </summary>
        public const string MinRuntimeVersionIsModelVersion = "UseRuntimeVersion";

        /// <summary>
        /// Initializes a new setup engine attribute that declares a required engine
        /// (this must be declared on a runtime assembly).
        /// </summary>
        /// <param name="assemblyName">Name of the engine assembly.</param>
        /// <param name="minRuntimeversion">
        /// The minimal Version for the Engine.
        /// Setting it to null removes all version constraints and setting it to the special
        /// value <see cref="UseRuntimeVersion"/> should be used only when the Runtime is the default, 
        /// primary one of the Engine.
        /// </param>
        public IsRuntimeThatUsesEngineAttribute( string assemblyName, string minRuntimeversion )
        {
            AssemblyName = assemblyName;
            MinRuntimeversion = minRuntimeversion;
        }

        /// <summary>
        /// Gets the name of the engine assembly.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Gets the minimal version for the engine assembly.
        /// When null, it removes all version constraints.
        /// Use the special value <see cref="UseRuntimeVersion"/> when the Runtime is the default, 
        /// primary one of the Engine.
        /// </summary>
        public string MinRuntimeversion { get; }
    }
}
