using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Extends the <see cref="SetupStep"/> to support the "Content" of a container.
    /// </summary>
    public enum SetupCallContainerStep
    {
        None = 0,
        Init,
        InitContent,
        Install,
        InstallContent,
        Settle,
        SettleContent
    }
}
