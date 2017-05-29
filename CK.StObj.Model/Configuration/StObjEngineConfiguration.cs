using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Encapsulates configuration for the StObj layer itself.
    /// </summary>
    public sealed class StObjEngineConfiguration
    {
        readonly BuildAndRegisterConfiguration _buildConfig;
        readonly BuilderFinalAssemblyConfiguration _finalConfig;

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public StObjEngineConfiguration()
        {
            _buildConfig = new BuildAndRegisterConfiguration();
            _finalConfig = new BuilderFinalAssemblyConfiguration();
        }

        /// <summary>
        /// Gets the configuration that describes how Application Domain must be used during build and
        /// which assemlies and types must be discovered.
        /// </summary>
        public BuildAndRegisterConfiguration BuildAndRegisterConfiguration => _buildConfig;

        /// <summary>
        /// Gets the configuration related to final assembly generation.
        /// </summary>
        public BuilderFinalAssemblyConfiguration FinalAssemblyConfiguration => _finalConfig;

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of IDependentItem) must be send to the monitor before sorting.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterInput { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of ISortedItem) must be send to the monitor once the graph is sorted.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterOutput { get; set; }

    }
}
