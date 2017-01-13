using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
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

}
