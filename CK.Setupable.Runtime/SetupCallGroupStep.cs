#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\SetupCallGroupStep.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup;

/// <summary>
/// Extends the <see cref="SetupStep"/> to support the "Content" of a Group.
/// </summary>
public enum SetupCallGroupStep
{
    /// <summary>
    /// Non applicable.
    /// </summary>
    None = 0,

    /// <summary>
    /// Initialization step: the first step of the setup process.
    /// </summary>
    Init = 1,

    /// <summary>
    /// Initialization step, after the container content.
    /// </summary>
    InitContent = 2,

    /// <summary>
    /// Install step: the second step of the setup process.
    /// </summary>
    Install = 3,

    /// <summary>
    /// Install step, after the container content.
    /// </summary>
    InstallContent = 4,

    /// <summary>
    /// Settle step: third and last step of the setup process.
    /// </summary>
    Settle = 5,

    /// <summary>
    /// Settle step, after the container content.
    /// Very last step of the setup process for a container.
    /// </summary>
    SettleContent = 6
}
