using System;

namespace CK.Core
{
    /// <summary>
    /// Extends <see cref="IContextualTypeMap"/> to expose Type to Object resolution.
    /// </summary>
    public interface IContextualObjectMap : IContextualTypeMap
    {
        /// <summary>
        /// Gets the structured object or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type (that supports IAmbientContract).</param>
        /// <returns>Structured object instance or null if the type has not been mapped.</returns>
        object Obtain( Type t );
        
        /// <summary>
        /// Gets the structured object or null if no mapping exists.
        /// </summary>
        /// <typeparam name="T">Key type (that supports IAmbientContract).</typeparam>
        /// <returns>Structured object instance or null if the type has not been mapped.</returns>
        T Obtain<T>() where T : class;

    }
}
