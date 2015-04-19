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
    public interface ISetupEngine
    {
        /// <summary>
        /// Gets the <see cref="ISetupEngineAspect"/> that participate to setup.
        /// </summary>
        IReadOnlyList<ISetupEngineAspect> Aspects { get; }

        /// <summary>
        /// Gets the first typed aspect that is assignable to <typeparamref name="T"/>. 
        /// If such aspect can not be found, a <see cref="CKException"/> is thrown.
        /// </summary>
        /// <typeparam name="T">Type of the aspect to obtain.</typeparam>
        /// <returns>The first compatible aspect.</returns>
        T Aspect<T>();

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
        /// Gives access to the ordered list of the <see cref="GenericItemSetupDriver"/>.
        /// </summary>
        IDriverList Drivers { get; }

        /// <summary>
        /// Gives access to the ordered list of all the <see cref="DriverBase"/> that participate to Setup.
        /// This list is filled after <see cref="RegisterSetupEvent"/> (and <see cref="SetupEvent"/> with <see cref="SetupStep.PreInit"/>) and before <see cref="SetupStep.Init"/>.
        /// </summary>
        IDriverBaseList AllDrivers { get; }

        /// <summary>
        /// Gets all ordered setup items without heads: a group or a container appears after the setup items it contains.
        /// </summary>
        IEnumerable<ISetupItem> AllItems { get; }

        /// <summary>
        /// Gets the current state of the engine.
        /// </summary>
        SetupEngineState State { get; }
    }
}
