using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines the immutable aspect of Context-Location-Name triplet.
    /// </summary>
    public interface IContextLocNaming
    {
        /// <summary>
        /// Gets the context part of this name.
        /// Can be null (unknown context) or empty (default context).
        /// </summary>
        string Context { get; }

        /// <summary>
        /// Gets the location part of this name. 
        /// Can be null (unknown location) or empty (root location).
        /// </summary>
        string Location { get; }

        /// <summary>
        /// Gets the name part (without <see cref="Context"/> nor <see cref="Location"/>).
        /// Must not be null.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the full name composed of <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/>.
        /// Must not be null.
        /// </summary>
        string FullName { get; }
    }
}
