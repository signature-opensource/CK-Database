using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{

    /// <summary>
    /// Exposes type mapping for a <see cref="Context"/>.
    /// </summary>
    public interface IAmbientTypeContextualMapper
    {
        /// <summary>
        /// Offers access to the <see cref="IAmbientTypeMapper"/> to which this contextual mapper belongs.
        /// </summary>
        IAmbientTypeMapper Owner { get; }

        /// <summary>
        /// Gets the context. <see cref="String.Empty"/> for the default context.
        /// </summary>
        string Context { get; }

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
        /// Retrieves the highest (most general) class that implements the given ambient contract interface.
        /// Null if no implementation exists for the interface.
        /// </summary>
        /// <param name="ambientContractInterface">Must be an interface that extends <see cref="IAmbientContract"/>.</param>
        /// <returns>The highest implementation or null.</returns>
        Type HighestImplementation( Type ambientContractInterface );

        /// <summary>
        /// Retrieves the highest (most general) class that implements the given ambient contract interface.
        /// </summary>
        /// <typeparam name="T">Must be an interface that extends <see cref="IAmbientContract"/>.</typeparam>
        /// <returns>The highest implementation or null.</returns>
        Type HighestImplementation<T>() where T : class, IAmbientContract;

        /// <summary>
        /// Gets whether a type is mapped.
        /// </summary>
        /// <param name="t">Type to lookup.</param>
        /// <returns>True if <paramref name="t"/> is mapped in this context, false otherwise.</returns>
        bool IsMapped( Type t );
    }
}
