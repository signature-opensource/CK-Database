using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// This interface can be supported by attributes (typically <see cref="IAttributeAmbientContextBound"/>) or by the
    /// setup item itself in order to interact/configure the driver once it has been pre initialized.
    /// </summary>
    public interface ISetupItemDriverAware
    {
        /// <summary>
        /// Called by the Engine right after the driver has been pre initialized.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="driver">The driver associated to this item.</param>
        /// <returns>True on success. Returning false cancels the setup process.</returns>
        bool OnDriverPreInitialized( IActivityMonitor monitor, SetupItemDriver driver );

    }
}
