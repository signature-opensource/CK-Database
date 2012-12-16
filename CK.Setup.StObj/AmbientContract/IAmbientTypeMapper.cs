using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Exposes a multi contextual type mapping.
    /// </summary>
    public interface IAmbientTypeMapper
    {
        /// <summary>
        /// Gets the default type mapper, the one identified by <see cref="String.Empty"/>.
        /// </summary>
        IAmbientTypeContextualMapper Default { get; }

        /// <summary>
        /// Gets the different contexts (including <see cref="Default"/>).
        /// </summary>
        IReadOnlyCollection<IAmbientTypeContextualMapper> Contexts { get; }

        /// <summary>
        /// Gets the result for any context or null if no such context exist.
        /// </summary>
        /// <param name="context">Type that identifies a context (null is the same as <see cref="String.Empty"/>).</param>
        /// <returns>The result for the given context.</returns>
        IAmbientTypeContextualMapper FindContext( string context );
    }
}
