using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Defines the <see cref="IMutableReferenceType.StObjRequirementBehavior"/> values.
    /// </summary>
    public enum StObjRequirementBehavior
    {
        /// <summary>
        /// The reference is not necessarily an existing <see cref="IAmbiantContract"/> (a <see cref="IStObj"/>).
        /// </summary>
        None = 0,
        /// <summary>
        /// A warn will be emitted if the reference is not a <see cref="IStObj"/>.
        /// </summary>
        WarnIfNotStObj,
        /// <summary>
        /// The reference must be an existing <see cref="IAmbiantContract"/> (a <see cref="IStObj"/>).
        /// </summary>
        ErrorIfNotStObj
    }
}
