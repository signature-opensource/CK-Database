using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Describes an ambient property.
    /// </summary>
    public interface IMutableAmbientProperty : IMutableResolvableReference
    {
        /// <summary>
        /// Gets whether this ambient property exists for the corresponding object type (or is defined by one of its specialization).
        /// </summary>
        /// <param name="stObj">The <see cref="IStObjMutableItem"/> that may contain this property.</param>
        /// <returns>True if this property exists for the given "slice" of the object.</returns>
        bool IsDefinedFor( IStObjMutableItem stObj );

    }
}
