using CK.Core;
using System;

namespace CK.Testing
{
    /// <summary>
    /// Gives access to one or more StObjMaps by loading them from existing generated assemblies.
    /// </summary>
    public interface IStObjMapTestHelperCore
    {
        /// <summary>
        /// Gets the generated assembly name from "StObjMap/GeneratedAssemblyName" configuration.
        /// Defaults to <see cref="StObjEngineConfiguration.DefaultGeneratedAssemblyName"/>.
        /// </summary>
        string GeneratedAssemblyName { get; }

        /// <summary>
        /// Gets the <see cref="IStObjMap"/> from <see cref="GeneratedAssemblyName"/>.
        /// </summary>
        IStObjMap StObjMap { get; }

    }
}
