using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines options related to app domain configuration during setup phasis.
    /// </summary>
    [Serializable]
    public class StObjBuilderAppDomainConfiguration
    {
        /// <summary>
        /// Gets or sets whether the setup phasis must be executed in a new AppDomain. 
        /// </summary>
        public bool UseIndependentAppDomain { get; set; }

        /// <summary>
        /// Probe paths to use to discuver assemblies. Only use if <see cref="UseIndependentAppDomain"/> is set to true.
        /// </summary>
        public IList<string> ProbePaths { get; private set; }


        public StObjBuilderAppDomainConfiguration()
        {
            ProbePaths = new List<string>();
        }

    }
}
