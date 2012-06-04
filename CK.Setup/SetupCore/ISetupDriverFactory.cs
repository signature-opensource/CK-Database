using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ISetupDriverFactory
    {
        SetupDriver CreateDriver( Type driverType, SetupDriver.BuildInfo info );

        SetupDriverContainer CreateDriverContainer( Type containerType, SetupDriverContainer.BuildInfo info );
    }
}
