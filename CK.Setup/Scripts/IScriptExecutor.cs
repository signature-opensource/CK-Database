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
        /// Executes the script.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="driver">The item driver for which the script is executed.</param>
        /// <param name="script">The script to execute.</param>
        /// <returns>True on success, false to stop the setup process.</returns>
        bool ExecuteScript( IActivityLogger logger, SetupDriver driver, ISetupScript script );
    }
}
