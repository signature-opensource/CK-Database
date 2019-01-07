using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Poco factory.
    /// These interfaces are automatically implemented.
    /// </summary>
    public interface IPocoFactory<out T> : IAmbientContract where T : IPoco
    {
        /// <summary>
        /// Creates a new Poco instance.
        /// </summary>
        /// <returns>A new poco instance.</returns>
        T Create();

        /// <summary>
        /// Gets the type of the final, unified, poco.
        /// </summary>
        Type PocoClassType { get; }
    }
}
