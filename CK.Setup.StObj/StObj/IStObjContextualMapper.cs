using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Mapper for discovered typed objects (that are <see cref="IAmbientContract"/>) to 
    /// their associated <see cref="IStObj"/>.
    /// It is bound to a registration <see cref="Context"/> and encapsulates 
    /// ambient type <see cref="TypeMappings"/>.
    /// </summary>
    public interface IStObjContextualMapper
    {
        /// <summary>
        /// Offers access to the <see cref="IStObjMapper"/> to which this contextual mapper belongs.
        /// </summary>
        IStObjMapper Owner { get; }

        /// <summary>
        /// Gets the context. <see cref="String.Empty"/> for the default context.
        /// </summary>
        string Context { get; }

        /// <summary>
        /// Gets the number of <see cref="IStObj"/> mapped.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the <see cref="IAmbientTypeContextualMapper"/> for this <see cref="Context"/>.
        /// </summary>
        IAmbientTypeContextualMapper TypeMappings { get; }

        /// <summary>
        /// Gets all the <see cref="IStObj"/> for this context. Use <see cref="Find"/> to look for a StObj by its type.
        /// </summary>
        IReadOnlyCollection<IStObj> Items { get; }

        /// <summary>
        /// Gets the mapped <see cref="IStObj"/> or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type (that supports IAmbientContract). Can be null (null is returned).</param>
        /// <returns>StObj object or null if the type has not been mapped.</returns>
        IStObj Find( Type t );

        /// <summary>
        /// Gets the mapped <see cref="IStObj"/> for the given type or null if no mapping exists.
        /// </summary>
        /// <typeparam name="T">Key type (that supports IAmbientContract). Can be null (null is returned).</typeparam>
        /// <returns>StObj object or null if the type has not been mapped.</returns>
        IStObj Find<T>() where T : IAmbientContract;

        /// <summary>
        /// Gets the structured object or null if no mapping exists.
        /// </summary>
        /// <typeparam name="T">Key type (that supports IAmbientContract).</typeparam>
        /// <returns>Structured object instance or null if the type has not been mapped.</returns>
        T GetObject<T>() where T : class;

    }
}
