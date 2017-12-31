using CK.Core;
using System;

namespace CK.Testing.StObjMap
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

        /// <summary>
        /// Fires the first time the <see cref="StObjMap"/> must be loaded.
        /// </summary>
        event EventHandler StObjMapLoading;

        /// <summary>
        /// Loads a <see cref="IStObjMap"/> from existing generated assembly.
        /// Actual loading of the assembly is done only if the StObjMap is not already available.
        /// </summary>
        /// <returns>The map or null if an error occurred (the error is logged).</returns>
        IStObjMap LoadStObjMap( string assemblyName, bool withWeakAssemblyResolver = true );

    }
}
