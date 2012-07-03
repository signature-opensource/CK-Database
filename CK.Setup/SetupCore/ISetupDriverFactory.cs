using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ISetupDriverFactory
    {
        ItemDriver CreateDriver( Type driverType, ItemDriver.BuildInfo info );

        ContainerDriver CreateDriverContainer( Type containerType, ContainerDriver.BuildInfo info );
    }
}
