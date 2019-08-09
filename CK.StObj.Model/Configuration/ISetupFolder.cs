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
        /// Gets the path of the directory to process.
        /// </summary>
        string Directory { get; }

        /// <summary>
        /// Gets an optional target (output) directory where genreated files (assembly and/or sources)
        /// must be copied. When null, this <see cref="Directory"/> is used.
        /// </summary>
        string DirectoryTarget { get; }

        /// <summary>
        /// Gets whether the compilation should be skipped for this folder: no assembly (see <see cref="StObjEngineConfiguration.GeneratedAssemblyName"/>)
        /// will be compiled however source files can be available (see <see cref="GenerateSourceFiles"/>).
        /// </summary>
        bool SkipCompilation { get; }

        /// <summary>
        /// Gets whether generated source files should be generated.
        /// </summary>
        bool GenerateSourceFiles { get; }

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
        /// Gets a set of assembly qualified type names that must be excluded from  
        /// registration.
        /// </summary>
        HashSet<string> ExcludedTypes { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that are known to be singletons. 
        /// </summary>
        HashSet<string> ExternalSingletonTypes { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that are known to be scoped. 
        /// </summary>
        HashSet<string> ExternalScopedTypes { get; }


    }
}
