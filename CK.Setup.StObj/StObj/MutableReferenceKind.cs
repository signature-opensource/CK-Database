using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Describes the different kind of <see cref="MutableReference"/>.
    /// </summary>
    [Flags]
    public enum MutableReferenceKind
    {
        /// <summary>
        /// Not applicable.
        /// </summary>
        None = 0,

        /// <summary>
        /// Container reference.
        /// </summary>
        Container = 1,
        
        /// <summary>
        /// Requires reference.
        /// </summary>
        Requires = 2,

        /// <summary>
        /// RequiredBy reference.
        /// </summary>
        RequiredBy = 4,

        /// <summary>
        /// Group reference.
        /// </summary>
        Group = 8,

        /// <summary>
        /// Child reference.
        /// </summary>
        Child = 8,

        /// <summary>
        /// Parameter from Construct method. It is a considered as a Requires.
        /// </summary>
        ConstructParameter = 16,

        /// <summary>
        /// Ambient property.
        /// </summary>
        AmbientProperty = 32,

    }
}
