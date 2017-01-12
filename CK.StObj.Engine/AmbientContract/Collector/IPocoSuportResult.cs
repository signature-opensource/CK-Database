using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Associates to <see cref="IPoco"/> interface its final, unified, implementation 
    /// and its <see cref="IPocoFactory{T}"/> interface type.
    /// </summary>
    public interface IPocoInterfaceInfo
    {
        /// <summary>
        /// Gets the IPoco interface.
        /// </summary>
        Type PocoInterface { get; }

        /// <summary>
        /// Gets the concrete, final, unified Poco type.
        /// </summary>
        IPocoRootInfo Root { get; }

        /// <summary>
        /// Gets the <see cref="IPocoFactory{T}"/> where T is <see cref="PocoInterface"/> type.
        /// </summary>
        Type PocoFactoryInterface { get; }

    }

    /// <summary>
    /// Defines information for a unified Poco type.
    /// </summary>
    public interface IPocoRootInfo
    {
        /// <summary>
        /// Gets the final, unified, type that implements all <see cref="Interfaces"/>.
        /// </summary>
        Type PocoClass { get; }

        /// <summary>
        /// Gets the <see cref="IPocoInterfaceInfo"/> that this Poco implements.
        /// </summary>
        IReadOnlyList<IPocoInterfaceInfo> Interfaces { get; }

    }


    /// <summary>
    /// Exposes the result of <see cref="IPoco"/> interfaces support.
    /// </summary>
    public interface IPocoSupportResult
    {
        /// <summary>
        /// The final factory type.
        /// </summary>
        Type FinalFactory { get; }

        /// <summary>
        /// Gets the root Poco information.
        /// </summary>
        IReadOnlyList<IPocoRootInfo> Roots { get; }

        /// <summary>
        /// Gets the <see cref="IPocoInterfaceInfo"/> for any <see cref="IPoco"/> interface.
        /// </summary>
        /// <param name="pocoInterface">The IPoco interface.</param>
        /// <returns>Information about the interface. Null if not found.</returns>
        IPocoInterfaceInfo Find( Type pocoInterface );

    }
}
