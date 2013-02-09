using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines options related to Application Domain configuration used during setup phasis.
    /// </summary>
    [Serializable]
    public class BuilderAppDomainConfiguration
    {
        /// <summary>
        /// Gets or sets whether the setup phasis must be executed in a new Application Domain.
        /// Defaults to false.
        /// </summary>
        public bool UseIndependentAppDomain { get; set; }

        /// <summary>
        /// Probe paths to use to discover assemblies. Used only if <see cref="UseIndependentAppDomain"/> is set to true.
        /// </summary>
        public IList<string> ProbePaths { get; private set; }

        /// <summary>
        /// Gets the <see cref="AssemblyRegistererConfiguration"/> that describes assemblies that must participate (or not) to setup.
        /// </summary>
        public AssemblyRegistererConfiguration Assemblies { get; private set; }

        /// <summary>
        /// Initialize a new <see cref="BuilderAppDomainConfiguration"/>.
        /// </summary>
        public BuilderAppDomainConfiguration()
        {
            ProbePaths = new List<string>();
            Assemblies = new AssemblyRegistererConfiguration();
        }

    }
}
