using System;
using CK.Core;

namespace CK.Setup
{
    public interface IScriptExecutor
    {
        /// <summary>
        /// Executes the script.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="script">The script to execute.</param>
        /// <returns>True on success, false to stop the setup process.</returns>
        bool ExecuteScript( IActivityLogger logger, ISetupScript script );
    }
}
