using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// The different states of the <see cref="SetupCenter"/>.
    /// </summary>
    public enum SetupCenterState
    {
        None,
        Registered,
        Initialized,
        InitializationError,
        Installed,
        InstallationError,
        Settled,
        SettlementError
    }
}
