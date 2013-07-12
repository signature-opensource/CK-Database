using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Setup
{
    public interface IStObjTrackedAmbientPropertyInfo
    {
        /// <summary>
        /// Gets the <see cref="IStObjResult"/> that holds the property.
        /// </summary>
        IStObjResult Owner { get; }

        /// <summary>
        /// Gets the property information.
        /// </summary>
        PropertyInfo PropertyInfo { get; }
    }
}
