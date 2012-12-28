using System;

namespace CK.Core
{
    /// <summary>
    /// Exposes a multi contextual mapping from type to object (<see cref="IContextualObjectMap"/>).
    /// </summary>
    public interface IContextRoot
    {
        /// <summary>
        /// Gets the default context, the one identified by <see cref="String.Empty"/>.
        /// </summary>
        IContextualObjectMap Default { get; }

        /// <summary>
        /// Gets the different contexts (including <see cref="Default"/>).
        /// </summary>
        IReadOnlyCollection<IContextualObjectMap> Contexts { get; }

        /// <summary>
        /// Gets the <see cref="IContextualObjectMap"/> or null if context is unknown.
        /// </summary>
        /// <param name="context">Context name.</param>
        /// <returns>Contextual mapping or null if no such context exists.</returns>
        IContextualObjectMap FindContext( string context );
    }
}
