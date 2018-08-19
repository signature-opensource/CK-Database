using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Require attribute for <see cref="IAmbientService"/> implementation.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class AmbientServiceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new <see cref="AmbientServiceAttributeAttribute"/> that specifies the associated container.
        /// </summary>
        /// <param name="container">The type of the container to which this service is associated.</param>
        public AmbientServiceAttribute( Type container )
        {
            if( container == null ) throw new ArgumentNullException( nameof( container) );
            Container = container;
        }

        /// <summary>
        /// Gets the type of the container to which this service is associated.
        /// </summary>
        public Type Container { get; private set; }
    }
}
