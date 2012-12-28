using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ISetupDriverFactory
    {
        SetupDriver CreateDriver( Type containerType, SetupDriver.BuildInfo info );
    }
}
