using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CK.Core
{
    /// <summary>
    /// Encapsulates configuration of the StObjEngine.
    /// </summary>
    public sealed partial class StObjEngineConfiguration : ISetupFolder
    {
        /// <summary>
        /// Default assembly name.
        /// </summary>
        public const string DefaultGeneratedAssemblyName = "CK.StObj.AutoAssembly";

        /// <summary>
        /// Gets the mutable list of all configuration aspects that must participate to setup.
        /// </summary>
        public List<IStObjEngineAspectConfiguration> Aspects { get; }

        /// <summary>
        /// Gets or sets the final Assembly name.
        /// When set to null (the default), <see cref="DefaultGeneratedAssemblyName"/> "CK.StObj.AutoAssembly" is returned.
        /// This is a global configuration that applies to all the <see cref="SetupFolders"/>.
        /// </summary>
        public string GeneratedAssemblyName
        {
            get => String.IsNullOrWhiteSpace(_generatedAssemblyName) ? DefaultGeneratedAssemblyName : _generatedAssemblyName;
            set => _generatedAssemblyName = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Diagnostics.FileVersionInfo.ProductVersion"/> of
        /// the <see cref="GeneratedAssemblyName"/> assembly or assemblies.
        /// Defaults to null (no <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/> should be generated).
        /// This is a global configuration that applies to all the <see cref="SetupFolders"/>.
        /// </summary>
        public string InformationalVersion { get; set; }

        /// <summary>
        /// Gets ors sets whether the ordering of StObj that share the same rank in the dependency graph must be inverted.
        /// Defaults to false.
        /// This is a global configuration that applies to all the <see cref="SetupFolders"/>.
        /// </summary>
        public bool RevertOrderingNames { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of IDependentItem) associated
        /// to the StObj objects must be send to the monitor before sorting.
        /// Defaults to false.
        /// This is a global configuration that applies to all the <see cref="SetupFolders"/>.
        /// </summary>
        public bool TraceDependencySorterInput { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of ISortedItem) associated
        /// to the StObj objects must be send to the monitor once the graph is sorted.
        /// Defaults to false.
        /// This is a global configuration that applies to all the <see cref="SetupFolders"/>.
        /// </summary>
        public bool TraceDependencySorterOutput { get; set; }

        /// <summary>
        /// Gets a mutable list of optional <see cref="SetupFolder"/>.
        /// Their assemblies and explicit classes must be subsets of <see cref="Assemblies"/> and <see cref="Types"/>,
        /// and their excluded types must be a superset of this <see cref="ExcludedTypes"/> for this configuration to be valid.
        /// </summary>
        public IList<SetupFolder> SetupFolders { get; }

        /// <summary>
        /// Gets the <see cref="AppContext.BaseDirectory"/> since this where the whole setup process
        /// must be ran.
        /// </summary>
        public string Directory => AppContext.BaseDirectory;

        /// <summary>
        /// Gets or sets an optional target (output) directory where genreated files (assembly and/or sources)
        /// must be copied. When null, <see cref="AppContext.BaseDirectory"/> is used.
        /// </summary>
        public string DirectoryTarget { get; set; }

        /// <summary>
        /// Gets or sets whether the compilation should be skipped for this <see cref="AppContext.BaseDirectory"/> folder
        /// (and compiled assembly shouldn't be copied to <see cref="DirectoryTarget"/>).
        /// Defaults to false.
        /// </summary>
        public bool SkipCompilation { get; set; }

        /// <summary>
        /// Gets or sets whether generated source files should be generated for this <see cref="AppContext.BaseDirectory"/> folder
        /// and copied to <see cref="DirectoryTarget"/>.
        /// Defaults to true.
        /// </summary>
        public bool GenerateSourceFiles { get; set; }

        /// <summary>
        /// Gets a set of assembly names that must be processed in <see cref="AppContext.BaseDirectory"/> for setup.
        /// Only assemblies that appear in this list will be considered.
        /// This list must be a superset of the <see cref="SetupFolders"/>' <see cref="SetupFolder.Assemblies"/>: no SetupFolder
        /// can involve an assembly that wouldn't appear in this list.
        /// </summary>
        public HashSet<string> Assemblies { get; }

        /// <summary>
        /// List of assembly qualified type names that must be explicitly registered in this <see cref="AppContext.BaseDirectory"/>
        /// folder regardless of <see cref="Assemblies"/>.
        /// This list must be a superset of the <see cref="SetupFolders"/>' <see cref="SetupFolder.Types"/>: no SetupFolder
        /// can explicitly register a Type that wouldn't appear in this list.
        /// </summary>
        public HashSet<string> Types { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that must be excluded from registration in <see cref="AppContext.BaseDirectory"/>.
        /// This list must be a subset of the <see cref="SetupFolders"/>' <see cref="SetupFolder.ExcludedTypes"/>: all SetupFolder must
        /// also exclude at least these types.
        /// </summary>
        public HashSet<string> ExcludedTypes { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that are known to be singletons.
        /// <para>
        /// There is no constraint between this root (<see cref="AppContext.BaseDirectory"/> folder) set and
        /// any of the <see cref="SetupFolders"/>' <see cref="SetupFolder.ExternalSingletonTypes"/>
        /// or <see cref="SetupFolder.ExternalScopedTypes"/>: for some SetupFolder a service may be a singleton
        /// whereas for another one the same service type may be implemented as a Scoped one.
        /// </para>
        /// </summary>
        public HashSet<string> ExternalSingletonTypes { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that are known to be scoped. 
        /// <para>
        /// There is no constraint between this root (<see cref="AppContext.BaseDirectory"/> folder) set and
        /// any of the <see cref="SetupFolders"/>' <see cref="SetupFolder.ExternalSingletonTypes"/>
        /// or <see cref="SetupFolder.ExternalScopedTypes"/>: for some SetupFolder a service may be a singleton
        /// whereas for another one the same service type may be implemented as a Scoped one.
        /// </para>
        /// </summary>
        public HashSet<string> ExternalScopedTypes { get; }


    }
}
