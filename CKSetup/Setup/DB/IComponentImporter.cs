using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    /// <summary>
    /// Provides an importable stream of components.
    /// </summary>
    public interface IComponentImporter
    {
        /// <summary>
        /// Opens a readable stream with available components.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="missing">Description of what should be obtained.</param>
        /// <returns>A readable stream on success, null on error.</returns>
        Stream OpenImportStream( IActivityMonitor monitor, ComponentMissingDescription missing );
    }
}
