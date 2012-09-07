using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Describes an ambiant property.
    /// </summary>
    public interface IMutableAmbiantProperty : IMutableResolvableReference
    {
        /// <summary>
        /// Tests whether this ambiant property exists for the corresponding object type.
        /// </summary>
        /// <param name="stObj">The <see cref="IStObj"/> that may contain this property.</param>
        /// <returns>True if this property exists for the IStObj slice of the object.</returns>
        bool IsDefinedFor( IStObj stObj );

    }
}
