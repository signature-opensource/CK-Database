using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Core abstraction of Setup engine.
    /// </summary>
    public interface ISetupEngine
    {
        /// <summary>
        /// Triggered for each steps of <see cref="SetupStep"/>: None (before registration), Init, Install, Settle and Done.
        /// </summary>
        event EventHandler<SetupEventArgs> SetupEvent;

        /// <summary>
        /// Triggered for each <see cref="DriverBase"/> setup phasis.
        /// </summary>
        event EventHandler<DriverEventArgs> DriverEvent;
        
        /// <summary>
        /// Gets the <see cref="ISetupSessionMemory"/> service that is used to persist any state related to setup phasis.
        /// It is a simple key-value dictionary where key is a string not longer than 255 characters and value is a non null string.
        /// </summary>
        ISetupSessionMemory Memory { get; } 

        /// <summary>
        /// Logger that will be used during setup.
        /// </summary>
        IActivityLogger Logger { get; }

        /// <summary>
        /// Gives access to the ordered list of all the <see cref="DriverBase"/> that participate to Setup.
        /// </summary>
        IDriverList AllDrivers { get; }

        /// <summary>
        /// Gets the current state of the engine.
        /// </summary>
        SetupEngineState State { get; }
    }
}
