using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Exposes a multi contextual type mapping.
    /// </summary>
    public interface IAmbientTypeMapper : IReadOnlyCollection<IAmbientTypeContextualMapper>
    {
        /// <summary>
        /// Gets the default type mapper, the one bound to <see cref="AmbientContractCollector.DefaultContext"/>.
        /// </summary>
        IAmbientTypeContextualMapper Default { get; }

        /// <summary>
        /// Gets the <see cref="IAmbientTypeContextualMapper"/> or null if context is unknown.
        /// </summary>
        /// <param name="typedContext">Typed context.</param>
        /// <returns>Contextual mapping or null if no such context exists.</returns>
        IAmbientTypeContextualMapper this[Type typedContext] { get; }
    }
}
