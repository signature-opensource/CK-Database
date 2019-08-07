using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Main interface that offers access to type mapping and Ambient Object instances.
    /// </summary>
    public interface IStObjMap
    {
        /// <summary>
        /// Gets the StObjs map.
        /// This is for advanced use: <see cref="ConfigureServices(IActivityMonitor, IServiceCollection)"/> handles
        /// everything that needs to be done before using all the services and objects.
        /// </summary>
        IStObjObjectMap StObjs { get; }

        /// <summary>
        /// Gets the Services map.
        /// This is for advanced use: <see cref="ConfigureServices(IActivityMonitor, IServiceCollection)"/> handles
        /// everything that needs to be done before using all the services and objects.
        /// </summary>
        IStObjServiceMap Services { get; }

        /// <summary>
        /// Gets the name of this StObj map.
        /// Never null, defaults to the empty string.
        /// </summary>
        string MapName { get; }

    }
}
