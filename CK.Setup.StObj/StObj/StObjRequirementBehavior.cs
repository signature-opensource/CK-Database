using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Defines the <see cref="IMutableReference.StObjRequirementBehavior"/> values.
    /// </summary>
    public enum StObjRequirementBehavior
    {
        /// <summary>
        /// The reference is not necessarily an existing <see cref="IAmbiantContract"/> (a <see cref="IStObj"/>).
        /// if an existing IStObj can not be found, the <see cref="IStObjDependencyResolver"/> is automatically sollicited.
        /// </summary>
        None = 0,

        /// <summary>
        /// A warn is emitted if the reference is not a <see cref="IStObj"/>, and the <see cref="IStObjDependencyResolver"/>
        /// is sollicited.
        /// </summary>
        WarnIfNotStObj,

        /// <summary>
        /// The reference must be an existing <see cref="IAmbiantContract"/> (a <see cref="IStObj"/>).
        /// </summary>
        ErrorIfNotStObj,

        /// <summary>
        /// The reference must be satisfied only by <see cref="IStObjDependencyResolver"/>. 
        /// Any existing <see cref="IAmbiantContract"/> (a <see cref="IStObj"/>) that could do the job are ignored.
        /// </summary>
        ExternalReference
    }
}
