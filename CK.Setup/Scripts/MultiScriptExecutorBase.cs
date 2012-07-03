using CK.Core;
using System.Collections.Generic;
using System;

namespace CK.Setup
{
    public abstract class MultiScriptExecutorBase : IScriptExecutor
    {
        /// <summary>
        /// Instanciates a <see cref="MultiScriptBase"/> thanks to <see cref="CreateMultiScript"/> 
        /// and <see cref="MultiScriptBase.ExecuteScript">executes</see> it.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="script">The script to execute.</param>
        /// <returns>True on success, false to stop the setup process.</returns>
        public virtual bool ExecuteScript( IActivityLogger logger, ISetupScript script )
        {
            MultiScriptBase m = CreateMultiScript( logger, script );
            return m != null ? m.ExecuteScript() : false;
        }

        /// <summary>
        /// Must create a new <see cref="MultiScriptBase"/>.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="script">The script to process.</param>
        /// <returns>
        /// A ready to run <see cref="MultiScriptBase"/> or null if it is not possible for any reason to execute the script. 
        /// In such case, an error or a fatal error SHOULD have been logged since this will stop the setup process.
        /// </returns>
        protected abstract MultiScriptBase CreateMultiScript( IActivityLogger logger, ISetupScript script );

    }
}
