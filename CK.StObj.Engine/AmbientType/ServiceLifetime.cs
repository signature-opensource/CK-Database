using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines the 2 life times and invalid combination of both.
    /// </summary>
    [Flags]
    public enum ServiceLifetime
    {
        /// <summary>
        /// Not a service we handle.
        /// </summary>
        None,

        /// <summary>
        /// Ambient service flag. 
        /// </summary>
        IsAmbientService = 1,

        /// <summary>
        /// Singleton flag.
        /// </summary>
        IsSingleton = 2,

        /// <summary>
        /// Scoped flag.
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
