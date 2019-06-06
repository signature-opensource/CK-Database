using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Extends <see cref="IStObjTypeMap"/> to expose <see cref="IStObj"/> and Type to Object resolution.
    /// </summary>
    public interface IStObjObjectMap : IStObjTypeMap
    {
        /// <summary>
        /// Gets the most specialized <see cref="IStObj"/> or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type.</param>
        /// <returns>Most specialized StObj or null if no mapping exists for this type.</returns>
        IStObj ToLeaf( Type t );

        /// <summary>
        /// Gets the structured object final implementation or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type (that must be an Ambient Object).</param>
        /// <returns>Structured object instance or null if the type has not been mapped.</returns>
        object Obtain( Type t );

        /// <summary>
        /// Gets all the structured object final implementations that exist in this context.
        /// </summary>
        IEnumerable<object> Implementations { get; }

        /// <summary>
        /// Gets all the <see cref="IStObj"/> and their final implementation that exist in this context.
        /// This contains only classes, not <see cref="IAmbientObject"/> interfaces. 
        /// Use <see cref="Mappings"/> to dump all the types to implementation mappings.
        /// </summary>
        IEnumerable<StObjImplementation> StObjs { get; }

        /// <summary>
        /// Gets all the <see cref="IAmbientObject"/> types to implementation objects that this
        /// context contains.
        /// The key types are interfaces (IAmbientObject) as well as classes.
        /// </summary>
        IEnumerable<KeyValuePair<Type, object>> Mappings { get; }


    }
}
