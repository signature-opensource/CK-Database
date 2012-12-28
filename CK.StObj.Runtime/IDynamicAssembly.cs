using System;
using System.Reflection.Emit;

namespace CK.Core
{
    /// <summary>
    /// Manages dynamic assembly creation with one <see cref="ModuleBuilder"/>.
    /// </summary>
    public interface IDynamicAssembly
    {
        /// <summary>
        /// Gets the <see cref="ModuleBuilder"/> for this <see cref="IDynamicAssembly"/>.
        /// </summary>
        ModuleBuilder ModuleBuilder { get; }

        /// <summary>
        /// Provides a new unique number that can be used for generating unique names inside this dynamic assembly.
        /// </summary>
        /// <returns>A unique number.</returns>
        string NextUniqueNumber();
    }
}
