using System;

namespace CK.Core
{
    public static class ServiceContainerExtension
    {
        /// <summary>
        /// Gets whether a service is available.
        /// </summary>
        /// <typeparam name="T">Type of the service.</typeparam>
        /// <param name="this">This container.</param>
        /// <returns>True if the service is available, false otherwise.</returns>
        public static bool IsAvailable<T>( this ISimpleServiceContainer @this ) => IsAvailable( @this, typeof(T) );

        /// <summary>
        /// Gets whether a service is available.
        /// </summary>
        /// <param name="this">This container.</param>
        /// <param name="serviceType">Service type.</param>
        /// <returns>True if the service is available, false otherwise.</returns>
        public static bool IsAvailable( this ISimpleServiceContainer @this, Type serviceType ) => @this.GetService( serviceType ) != null;
    }
}
