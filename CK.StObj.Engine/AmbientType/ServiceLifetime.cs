using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines the 2 life times and invalid combination of both.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// Not a service we handle.
        /// </summary>
        None,

        /// <summary>
        /// This is an ambient service. 
        /// </summary>
        Ambient = 1,

        /// <summary>
        /// Service is a singleton. 
        /// </summary>
        Singleton = Ambient | 2,

        /// <summary>
        /// Service is a scoped one.
        /// </summary>
        Scope = Ambient | 4,

        /// <summary>
        /// Singleton and Scope is an error.
        /// </summary>
        BothError = Singleton | Scope
    }
}
