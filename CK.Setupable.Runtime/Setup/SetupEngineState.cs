using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// The different states of the <see cref="ISetupEngine"/>.
    /// </summary>
    [Flags]
    public enum SetupEngineState
    {
        None = 0,
        Registered = 1,
        Initialized = Registered|2,
        InitializationError = Initialized | Error,
        Installed = Initialized | 4,
        InstallationError = Installed | Error,
        Settled = Installed | 8,
        SettlementError = Settled | Error,
        Disposed = 16,

        Error = 64
    }
}
