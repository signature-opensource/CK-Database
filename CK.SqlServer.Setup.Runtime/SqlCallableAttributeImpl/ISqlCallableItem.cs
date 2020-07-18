using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    internal interface ISqlCallableItem : ISetupItem
    {
        /// <summary>
        /// Gets the sql object.
        /// </summary>
        ISqlServerCallableObject CallableObject { get; }

        /// <summary>
        /// Gets (and generates) the method that creates the SqlCommand for this callable item.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="dynamicAssembly">Use the memory associated to the dynamic to share the static class that implements the creation methods.</param>
        /// <returns>The function. Null if an error occurred while generating it.</returns>
        IFunctionScope AssumeSourceCommandBuilder( IActivityMonitor monitor, IDynamicAssembly dynamicAssembly );
    }

}
