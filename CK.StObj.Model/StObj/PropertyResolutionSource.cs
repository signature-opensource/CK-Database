using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Defines how a property value will be searched if not set explicitely set.
    /// </summary>
    public enum PropertyResolutionSource
    {
        /// <summary>
        /// Property is not resolved from container nor generalization.
        /// This should be rarely used.
        /// </summary>
        None,

        /// <summary>
        /// Property is resolved first from the Container and, if not found, from the Generalization.
        /// This is the default for <see cref="StObjPropertyAttribute">StObj Properties</see>.
        /// </summary>
        FromContainerAndThenGeneralization,

        /// <summary>
        /// Property is resolved first from the Generalization and, if not found, from its Containers.
        /// This is the default for <see cref="AmbientPropertyAttribute">AmbientProperty</see> and <see cref="AmbientContractPropertyAttribute">AmbientContract</see> properties.
        /// </summary>
        FromGeneralizationAndThenContainer
    }
}
