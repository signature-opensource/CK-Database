using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Exposes a multi contextual type mapping.
    /// </summary>
    public interface IStObjMapper : IReadOnlyCollection<IStObjContextualMapper>
    {
        /// <summary>
        /// Gets the default mapper, the one identified by <see cref="String.Empty"/>.
        /// </summary>
        IStObjContextualMapper Default { get; }

        /// <summary>
        /// Gets the <see cref="IStObjContextualMapper"/> or null if context is unknown.
        /// </summary>
        /// <param name="context">Context name.</param>
        /// <returns>Contextual mapping or null if no such context exists.</returns>
        IStObjContextualMapper this[string context] { get; }
    }
}
