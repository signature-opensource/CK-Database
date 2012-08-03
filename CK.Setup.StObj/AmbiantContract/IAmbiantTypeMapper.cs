using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{

    /// <summary>
    /// Exposes a contextual type mapping.
    /// </summary>
    public interface IAmbiantTypeMapper
    {
        /// <summary>
        /// Gets the context. Null for the default context.
        /// </summary>
        Type Context { get; }

        /// <summary>
        /// Gets the number of type mapped.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the mapped type or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type.</param>
        /// <returns>Mapped type or null if no mapping exists for this type.</returns>
        Type this[Type t] { get; }

        /// <summary>
        /// Retrieves the highest (most general) class that implements the given ambiant contract interface.
        /// Null if no implementation exists for the interface.
        /// </summary>
        /// <param name="ambiantContractInterface">Must be an interface that extends <see cref="IAmbiantContract"/>.</param>
        /// <returns>The highest implementation or null.</returns>
        Type HighestImplementation( Type ambiantContractInterface );

        /// <summary>
        /// Retrieves the highest (most general) class that implements the given ambiant contract interface.
        /// </summary>
        /// <typeparam name="T">Must be an interface that extends <see cref="IAmbiantContract"/>.</typeparam>
        /// <returns>The highest implementation or null.</returns>
        Type HighestImplementation<T>() where T : class, IAmbiantContract;

        /// <summary>
        /// Gets whether a type is mapped.
        /// </summary>
        /// <param name="t">Type to lookup.</param>
        /// <returns>True if <paramref name="t"/> is mapped, false otherwise.</returns>
        bool IsMapped( Type t );
    }
}
