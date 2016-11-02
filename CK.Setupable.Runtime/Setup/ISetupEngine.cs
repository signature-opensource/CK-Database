#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\ISetupEngine.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
    public interface ISetupEngine : ISetupEngineAspectProvider
    {
        /// <summary>
        /// Triggered before registration (at the beginning of the Register step).
        /// This event fires before the <see cref="SetupEvent"/> (with <see cref="SetupEventArgs.Step"/> set to None), and enables
        /// registration of setup items.
        /// </summary>
        event EventHandler<RegisterSetupEventArgs> RegisterSetupEvent;

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
        /// Monitor that will be used during setup.
        /// </summary>
        IActivityMonitor Monitor { get; }

        /// <summary>
        /// Gives access to an ordered list of <see cref="SetupItemDriver"/> indexed by the <see cref="IDependentItem.FullName"/> 
        /// or by the <see cref="IDependentItem"/> object instance itself.
        /// </summary>
        IDriverList Drivers { get; }

        /// <summary>
        /// Gives access to an ordered list of <see cref="DriverBase"/> indexed by the <see cref="IDependentItem.FullName"/> 
        /// or by the <see cref="IDependentItem"/> object instance itself that participate to Setup.
        /// This list contains all the <see cref="SetupItemDriver"/> plus all the internal drivers for the head of Groups 
        /// or Containers (the ones that ar not SetupItemDriver instances and have a <see cref="DriverBase.FullName"/> that
        /// ends with ".Head").
        /// </summary>
        IDriverBaseList AllDrivers { get; }

        /// <summary>
        /// Gets the current state of the engine.
        /// </summary>
        SetupEngineState State { get; }
    }
}
