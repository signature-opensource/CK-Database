using System;
using System.Collections;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using CK.Reflection;

namespace CK.SqlServer.Setup
{
    internal interface ISqlCallableItem : ISetupItem
    {
        /// <summary>
        /// Gets the sql object.
        /// </summary>
        ISqlServerCallableObject CallableObject { get; }

        /// <summary>
        /// Gets or generates the method that creates the <see cref="SqlCommand"/> for this callable item.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="dynamicAssembly">Use the memory associated to the dynamic to share the static class that implements the creation methods
        /// and the PushFinalAction to actually create it.</param>
        /// <returns>The method info. Null if <see cref="IsValid"/> is false or if an error occurred while generating it.</returns>
        MethodInfo AssumeCommandBuilder( IActivityMonitor monitor, IDynamicAssembly dynamicAssembly );
    }

}
