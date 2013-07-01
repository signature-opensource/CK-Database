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
        Type ToLeafType( Type t );

        /// <summary>
        /// Gets whether a type is mapped.
        /// </summary>
        /// <param name="t">Type to lookup.</param>
        /// <returns>True if <paramref name="t"/> is mapped in this context, false otherwise.</returns>
        bool IsMapped( Type t );

        IContextualRoot<IContextualTypeMap> AllContexts { get; }
    }
}
