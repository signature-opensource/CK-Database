#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\IScriptExecutor.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Classes that implements this interface are managed by a <see cref="ScriptTypeHandler"/>.
    /// </summary>
    public interface IScriptExecutor
    {
        /// <summary>
        /// Implementation must execute the given script.
        /// </summary>
        /// <param name="_monitor">The _monitor to use.</param>
        /// <param name="driver">The item driver for which the script is executed.</param>
        /// <param name="script">The script to execute.</param>
        /// <returns>True on success, false to stop the setup process.</returns>
        bool ExecuteScript( IActivityMonitor monitor, GenericItemSetupDriver driver, ISetupScript script );
    }
}
