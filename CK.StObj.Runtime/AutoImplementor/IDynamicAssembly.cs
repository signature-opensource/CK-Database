using CK.CodeGen;
using System;
using System.Collections;
using System.Reflection.Emit;

namespace CK.Core
{
    /// <summary>
    /// Supports assembly generation.
    /// Actual support is not required by the model layer: runtime and engine are in charge of
    /// extending this abstraction in any required way.
    /// </summary>
    public interface IDynamicAssembly
    {
        /// <summary>
        /// Provides a new unique number that can be used for generating unique names inside this dynamic assembly.
        /// </summary>
        /// <returns>A unique number.</returns>
        string NextUniqueNumber();

        /// <summary>
        /// Gets a shared dictionary associated to the dynamic assembly. 
        /// Methods that generate code can rely on this to store shared information as required by their generation process.
        /// </summary>
        IDictionary Memory { get; }

        /// <summary>
        /// Gets the <see cref="ModuleBuilder"/> for this <see cref="IDynamicAssembly"/>.
        /// </summary>
        ModuleBuilder ModuleBuilder { get; }

        /// <summary>
        /// Gets the source builder for this <see cref="IDynamicAssembly"/>.
        /// </summary>
        NamespaceBuilder SourceBuilder { get; }

        /// <summary>
        /// Pushes an action that will be executed before the generation of the final assembly: use this to 
        /// create final type from a <see cref="TypeBuilder"/> or to execute any action that must be done at the end 
        /// of the generation process.
        /// An action can be pushed at any moment: a pushed action can push another action.
        /// </summary>
        /// <param name="postAction">Action to execute.</param>
        void PushFinalAction( Action<IDynamicAssembly> postAction );
    }

}
