using System;

namespace CK.Core
{
    /// <summary>
    /// Exposes a contextual type to type mapping.
    /// </summary>
    public interface IContextualTypeMap
    {
        /// <summary>
        /// Gets the name of the context.
        /// </summary>
        string Context { get; }

        /// <summary>
        /// Gets the number of type mapped.
        /// </summary>
        int MappedTypeCount { get; }

        /// <summary>
        /// Gets the mapped type or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type.</param>
        /// <returns>Mapped type or null if no mapping exists for this type.</returns>
        Type MapType( Type t );

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
