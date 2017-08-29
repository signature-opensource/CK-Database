#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\ISetupHandler.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Defines the entry points of handlers that are associated to a <see cref="SetupItemDriver"/>.
    /// This contract does not bind the handlers to a specific driver: the same handler instance
    /// can be <see cref="SetupItemDriver.AddHandler(ISetupHandler)">added</see> to multiple drivers.
    /// </summary>
    public interface ISetupHandler
    {
        /// <summary>
        /// Called during the <see cref="SetupStep.Init"/> phasis.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="d">The calling driver.</param>
        /// <returns>True on success, false on error. Returning false stops the process.</returns>
        bool Init( IActivityMonitor monitor, SetupItemDriver d );

        /// <summary>
        /// Called during the <see cref="SetupStep.Install"/> phasis.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="d">The calling driver.</param>
        /// <returns>True on success, false on error. Returning false stops the process.</returns>
        bool InitContent( IActivityMonitor monitor, SetupItemDriver d );

        /// <summary>
        /// Called during the <see cref="SetupStep.Install"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="d">The calling driver.</param>
        /// <returns>True on success, false on error. Returning false stops the process.</returns>
        bool Install( IActivityMonitor monitor, SetupItemDriver d );

        /// <summary>
        /// Called during the <see cref="SetupStep.Install"/> phasis for groups or container
        /// once their content has been installed.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="d">The calling driver.</param>
        /// <returns>True on success, false on error. Returning false stops the process.</returns>
        bool InstallContent( IActivityMonitor monitor, SetupItemDriver d );

        /// <summary>
        /// Called during the <see cref="SetupStep.Settle"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="d">The calling driver.</param>
        /// <returns>True on success, false on error. Returning false stops the process.</returns>
        bool Settle( IActivityMonitor monitor, SetupItemDriver d );

        /// <summary>
        /// Called during the <see cref="SetupStep.Settle"/> phasis for groups or container
        /// once their content has been settled.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="d">The calling driver.</param>
        /// <returns>True on success, false on error. Returning false stops the process.</returns>
        bool SettleContent( IActivityMonitor monitor, SetupItemDriver d );

        /// <summary>
        /// Called on at each step, right after its corresponding dedicated method.
        /// This centralized step based method is easier to use than the different
        /// available overrides when the step actions are structurally the same and
        /// only their actual contents/data is step dependent.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="d">The calling driver.</param>
        /// <param name="step">Current process step.</param>
        /// <returns>True on success, false on error. Returning false stops the process.</returns>
        bool OnStep( IActivityMonitor monitor, SetupItemDriver d, SetupCallGroupStep step );
    }
}
