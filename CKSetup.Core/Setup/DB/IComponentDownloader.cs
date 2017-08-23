using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    /// <summary>
    /// Used by <see cref="DependencyResolver"/> to obtain missing component
    /// during its resolution.
    /// </summary>
    public interface IComponentDownloader
    {
        /// <summary>
        /// Obtains missing components and imports them in <see cref="ComponentDB"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="missing">The components to download.</param>
        /// <returns>The updated database, null on error.</returns>
        ComponentDB Download( IActivityMonitor monitor, ComponentMissingDescription missing );
    }
}
