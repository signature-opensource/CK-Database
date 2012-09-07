using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Describes a read only ambiant property.
    /// </summary>
    public interface IAmbiantProperty
    {
        /// <summary>
        /// Gets the item that exposes this ambiant property.
        /// </summary>
        IStObj Owner { get; }
        
        /// <summary>
        /// Gets the typed context associated to the <see cref="P:Type"/> of this reference.
        /// </summary>
        Type Context { get; set; }

        /// <summary>
        /// Gets the type of the ambiant property.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the name of the ambiant property.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether this property can be considered as optional.
        /// When true the property is not set if resolution fails (ie. <see cref="Type.Missing"/> is obtained).
        /// </summary>
       bool IsOptional { get; }

    }
}
