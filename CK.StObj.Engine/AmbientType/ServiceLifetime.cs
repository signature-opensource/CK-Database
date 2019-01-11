using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines the 2 possible services life times, invalid combination of both
    /// and <see cref="IAmbientService"/> support.
    /// </summary>
    [Flags]
    public enum ServiceLifetime
    {
        /// <summary>
        /// Not a service we handle or external service for which
        /// no lifetime is known.
        /// </summary>
        None,

        /// <summary>
        /// Ambient service flag. 
        /// </summary>
        IsAmbientService = 1,

        /// <summary>
        /// Singleton flag.
        /// External services are flagged with this only.
        /// </summary>
        IsSingleton = 2,

        /// <summary>
        /// Scoped flag.
        /// External services are flagged with this only.
        /// </summary>
        IsScoped = 4,

        /// <summary>
        /// Ambient service is a singleton. 
        /// </summary>
        AmbientSingleton = IsAmbientService | IsSingleton,

        /// <summary>
        /// Ambient service is a scoped one.
        /// </summary>
        AmbientScope = IsAmbientService | IsScoped,

        /// <summary>
        /// Singleton and Scope is an error.
        /// </summary>
        AmbientBothError = AmbientSingleton | AmbientScope
    }
}
