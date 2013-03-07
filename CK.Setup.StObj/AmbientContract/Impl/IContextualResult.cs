using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Utility interface that defines the result of a processing scoped by a <see cref="Type"/> (the <see cref="Context"/> property).
    /// The utility class <see cref="MultiContextualResult{T}"/> provides common implementations for such contextualized results.
    /// </summary>
    public interface IContextualResult
    {
        /// <summary>
        /// Gets the context of this result.
        /// Never null since <see cref="String.Empty"/> designates the default context.
        /// </summary>
        string Context { get; }

        /// <summary>
        /// Gets whether an error exists that prevents the process to continue.
        /// </summary>
        /// <returns>
        /// False to continue the process (only warnings - or error considered as 
        /// warning - occured), true to stop remaining processes.
        /// </returns>
        bool HasFatalError { get; }
    }

}
