using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Setup
{
    public interface ITrackedAmbientPropertyInfo
    {
        /// <summary>
        /// Gets the <see cref="IStObj"/> that holds the property.
        /// </summary>
        IStObj SpecializedOwner { get; }

        /// <summary>
        /// Gets the property information.
        /// </summary>
        PropertyInfo PropertyInfo { get; }
    }
}
