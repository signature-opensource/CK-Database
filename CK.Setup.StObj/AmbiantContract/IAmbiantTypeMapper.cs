using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Exposes a multi contextual type mapping.
    /// </summary>
    public interface IAmbiantTypeMapper : IReadOnlyCollection<IAmbiantTypeContextualMapper>
    {
        /// <summary>
        /// Gets the default type mapper, the one bound to <see cref="AmbiantContractCollector.DefaultContext"/>.
        /// </summary>
        IAmbiantTypeContextualMapper Default { get; }

        /// <summary>
        /// Gets the <see cref="IAmbiantTypeContextualMapper"/> or null if context is unknown.
        /// </summary>
        /// <param name="typedContext">Typed context.</param>
        /// <returns>Contextual mapping or null if no such context exists.</returns>
        IAmbiantTypeContextualMapper this[Type typedContext] { get; }
    }
}
