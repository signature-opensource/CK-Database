#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\SetupEngineState.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Setup;

/// <summary>
/// The different states of the <see cref="SetupCoreEngine"/> engine.
/// </summary>
[Flags]
enum SetupCoreEngineState
{
    /// <summary>
    /// Aspect has not run yet.
    /// </summary>
    None = 0,
    /// <summary>
    /// Setup items have beed registered, ordered and the
    /// drivers have been created and pre initialized.
    /// </summary>
    Registered = 1,
    /// <summary>
    /// Drivers initialization have been called. 
    /// </summary>
    Initialized = Registered | 2,
    /// <summary>
    /// An error occurred during the <see cref="SetupStep.Init"/> phasis.
    /// </summary>
    InitializationError = Initialized | Error,
    /// <summary>
    /// Drivers installation have been called. 
    /// </summary>
    Installed = Initialized | 4,
    /// <summary>
    /// An error occurred during the <see cref="SetupStep.Install"/> phasis.
    /// </summary>
    InstallationError = Installed | Error,
    /// <summary>
    /// Drivers settlement have been called. 
    /// </summary>
    Settled = Installed | 8,
    /// <summary>
    /// An error occurred during the <see cref="SetupStep.Settle"/> phasis.
    /// </summary>
    SettlementError = Settled | Error,
    /// <summary>
    /// Drivers have been disposed.
    /// </summary>
    Disposed = 16,

    /// <summary>
    /// Error bit flag.
    /// </summary>
    Error = 64
}
