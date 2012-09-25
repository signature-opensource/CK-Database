using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Enables any attributes other than <see cref="SetupAttribute"/> and <see cref="SetupName"/> 
    /// to carry the full name of a setup object.
    /// </summary>
    public interface ISetupNameAttribute
    {
        /// <summary>
        /// Gets the full name of the setup object.
        /// </summary>
        string FullName { get; }
    }
}
