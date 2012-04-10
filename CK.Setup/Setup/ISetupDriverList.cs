using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// An ordered list of <see cref="SetupDriverBase"/> indexed by the <see cref="ISetupableItem.FullName"/>.
    /// </summary>
    public interface ISetupDriverList : IReadOnlyList<SetupDriverBase>
    {
        /// <summary>
        /// Gets a <see cref="SetupDriverBase"/> by its name.
        /// </summary>
        /// <param name="fullName">The item full name.</param>
        /// <returns>The associated driver or null if the driver does not exist.</returns>
        SetupDriverBase this[string fullName] { get; }
    }
}
