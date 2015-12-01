#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\SetupStep.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup
{
    /// <summary>
    /// Defines the 3 fundamental setup steps plus initial and final steps.
    /// </summary>
    public enum SetupStep
    {
        /// <summary>
        /// Initial step.
        /// </summary>
        PreInit = 0,
        /// <summary>
        /// Initialization step: the first step of the setup process.
        /// </summary>
        Init = 1,
        /// <summary>
        /// Install step: the second step of the setup process.
        /// </summary>
        Install = 3,
        /// <summary>
        /// Settle step: third and last step of the setup process.
        /// </summary>
        Settle = 5,
        /// <summary>
        /// Successful setup process (reached before <see cref="Disposed"/> step).
        /// </summary>
        Success = 7,
        /// <summary>
        /// Last step (reached for a successful setup process or on error).
        /// </summary>
        Disposed = 8
    }

}
