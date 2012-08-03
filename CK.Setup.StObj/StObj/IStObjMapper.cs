using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Mapper for discovered typed objects (that are <see cref="IAmbiantContract"/>) to 
    /// their associated <see cref="IStObjDependentItem"/>.
    /// It is bound to a registration <see cref="Context"/> and encapsulates 
    /// ambiant type <see cref="Mappings"/>.
    /// </summary>
    public interface IStObjMapper
    {
        /// <summary>
        /// Gets the context. Null for the default context.
        /// </summary>
        Type Context { get; }

        /// <summary>
        /// Gets the number of <see cref="IStObjDependentItem"/> mapped.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the <see cref="IAmbiantTypeMapper"/> for this <see cref="Context"/>.
        /// </summary>
        IAmbiantTypeMapper Mappings { get; }

        /// <summary>
        /// Gets the mapped <see cref="IStObjDependentItem"/> or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type. Can be null (null is returned).</param>
        /// <returns>Dependent item or null if the type has not been mapped.</returns>
        IStObjDependentItem this[Type t] { get; }

    }
}
