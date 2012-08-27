using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// An ordered list of <see cref="DriverBase"/> indexed by the <see cref="ISetupableItem.FullName"/>.
    /// </summary>
    public interface IDriverList : IReadOnlyList<DriverBase>
    {
        /// <summary>
        /// Gets a <see cref="DriverBase"/> by its name.
        /// </summary>
        /// <param name="fullName">The item full name.</param>
        /// <returns>The associated driver or null if the driver does not exist.</returns>
        DriverBase this[string fullName] { get; }

        /// <summary>
        /// Gets a <see cref="DriverBase"/> associated to a <see cref="IDependentItem"/>.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The associated driver or null if the driver does not exist.</returns>
        DriverBase this[ IDependentItem item ] { get; }

    }
}
