using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines the result of a processing scoped by a <see cref="Type"/> (the <see cref="Context"/> property).
    /// </summary>
    public interface IContextResult
    {
        /// <summary>
        /// Gets the context of this result.
        /// </summary>
        Type Context { get; }

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
