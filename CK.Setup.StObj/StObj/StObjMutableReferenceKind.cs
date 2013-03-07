using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Describes the different kind of <see cref="IStObjReference"/>.
    /// </summary>
    [Flags]
    public enum StObjMutableReferenceKind
    {
        /// <summary>
        /// Non applicable.
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
        Child = 16,

        /// <summary>
        /// Parameter from Construct method. It is a considered as a Requires.
        /// </summary>
        ConstructParameter = 32,

        /// <summary>
        /// Ambient property.
        /// This kind of reference can depend on the referenced StObj (see <see cref="TrackAmbientPropertiesMode"/>).
        /// </summary>
        AmbientProperty = 64,

        /// <summary>
        /// Pure reference to another object without any structural constraint.
        /// </summary>
        AmbientContract = 128
    }
}
