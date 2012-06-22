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
        /// Gets whether a type is mapped.
        /// </summary>
        /// <param name="t">Type to lookup.</param>
        /// <returns>True if <paramref name="t"/> is mapped, false otherwise.</returns>
        bool IsMapped( Type t );
    }
}
