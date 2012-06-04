using CK.Core;
using System.Collections.Generic;

namespace CK.Setup
{
    public interface IScriptTypeHandler
    {
        /// <summary>
        /// Gets the type of scripts that this script handler handles.
        /// </summary>
        string ScriptType { get; }

        /// <summary>
        /// Creates the object that will be in charge of script execution.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="container">The setup container for which an executor must be created.</param>
        /// <returns>A <see cref="IScriptExecutor"/> object.</returns>
        IScriptExecutor CreateExecutor( IActivityLogger logger, SetupDriverContainer container );

        /// <summary>
        /// Called by the framework to indicate that a <see cref="IScriptExecutor"/> is no longer needed.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="executor">The useless executor.</param>
        void Release( IActivityLogger logger, IScriptExecutor executor );

        /// <summary>
        /// Gets script types that must be executed before this one. 
        /// Use '?' prefix to specify that the handler is not required (like "?sql").
        /// Can be null if no such handler exists.
        /// </summary>
        IEnumerable<string> Requires { get; }

        /// <summary>
        /// Gets names of revert dependencies: scripts for this handler 
        /// will be executed before scripts for them. 
        /// Can be null if no such handler exists.
        /// </summary>
        IEnumerable<string> RequiredBy { get; }

    }
}
