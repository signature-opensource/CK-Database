using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Describes a folder to process.
    /// </summary>
    public interface ISetupFolder
    {
        /// <summary>
        /// Gets or the path of the directory.
        /// </summary>
        string Directory { get; }

        /// <summary>
        /// Gets a set of assembly names that must be processed for setup.
        /// Only assemblies that appear in this list will be considered.
        /// </summary>
        HashSet<string> Assemblies { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that must be explicitly registered 
        /// regardless of <see cref="Assemblies"/>.
        /// </summary>
        HashSet<string> Types { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that are known to be singletons. 
        /// </summary>
        HashSet<string> ExternalSingletonTypes { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that must be excluded from  
        /// registration.
        /// </summary>
        HashSet<string> ExcludedTypes { get; }

    }
}
