using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ISetupDriverFactory
    {
        /// <summary>
        /// Creates a (potentially configured) instance of <see cref="SetupDriver"/> of a given <paramref name="driverType"/>.
        /// </summary>
        /// <param name="driverType">SetupDriver type to create.</param>
        /// <param name="info">Internal constructor information.</param>
        /// <returns>A setup driver. Null if not able to create it (a basic <see cref="Activator.CreateInstance"/> will be used to create the driver).</returns>
        SetupDriver CreateDriver( Type driverType, SetupDriver.BuildInfo info );
    }
}
