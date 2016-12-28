using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Describes the <see cref="SetupEngineConfiguration.RunningMode"/>.
    /// </summary>
    public enum SetupEngineRunningMode
    {
        /// <summary>
        /// Normal process: StObj creation and three-steps setup.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Normal process: StObj creation and three-steps setup except that the ordering for setupable items that share the same rank 
        /// in the pure dependency graph is inverted. (See DependencySorter object in CK.Setup.Dependency assembly for more information.)
        /// </summary>
        RevertNames = 1,

        /// <summary>
        /// Does nothing except initializing configured aspects.
        /// </summary>
        InitializeEngineOnly = 2

    }
}
