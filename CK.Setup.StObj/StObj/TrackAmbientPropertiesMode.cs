using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public enum TrackAmbientPropertiesMode
    {
        /// <summary>
        /// Tracking mode is not applicable or is not known.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Ambient Properties are not tracked at all.
        /// </summary>
        None = 1,
        /// <summary>
        /// Consider Ambient Properties holder object as a child of this <see cref="IDependentItemGroup"/>.
        /// </summary>
        AddPropertyHolderAsChildren = 2,
        /// <summary>
        /// Consider Ambient Properties holder object as a Group for this <see cref="IDependentItem"/>.
        /// </summary>
        AddThisToPropertyHolderItems = 3,
        /// <summary>
        /// Consider Ambient Properties holder object as a requirement for this <see cref="IDependentItem"/>.
        /// </summary>
        PropertyHolderRequiresThis = 4,
        /// <summary>
        /// Consider Ambient Properties holder object to be required by this <see cref="IDependentItem"/>.
        /// </summary>
        PropertyHolderRequiredByThis = 5,
    }
}
