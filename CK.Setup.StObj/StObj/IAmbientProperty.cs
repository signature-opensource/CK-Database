using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Exposes a resolvable ambient property.
    /// </summary>
    public interface IAmbientProperty : IResolvableReference
    {
        /// <summary>
        /// Gets whether this ambient property exists for the corresponding object type (or is defined by one of its specialization).
        /// </summary>
        /// <param name="stObj">The <see cref="IStObj"/> that may contain this property.</param>
        /// <returns>True if this property exists for the given "slice" of the object.</returns>
        bool IsDefinedFor( IStObj stObj );
    }
}
