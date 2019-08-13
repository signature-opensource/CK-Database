using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Setup
{

    /// <summary>
    /// Reads versionning information of a system.
    /// </summary>
    public interface IVersionedItemReader
    {
        /// <summary>
        /// Called by the engine at the beginning of the setup process.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <returns>
        /// Should return all the versions for all the <see cref="IDependentItem.FullName"/> previously installed
        /// and the <see cref="VFeature"/> already registered.
        /// </returns>
        OriginalReadInfo GetOriginalVersions( IActivityMonitor monitor );

        /// <summary>
        /// Called by the engine when the version is not found for the item
        /// before using the <see cref="IVersionedItem.PreviousNames"/>.
        /// This is a "first chance" optional hook.
        /// This enables any possible mapping and fallback to take place.
        /// </summary>
        /// <param name="item">Item for which a version should be found.</param>
        /// <param name="originalVersions">
        /// A getter for original versions. This can help the implementation to avoid duplicating its own version
        /// of <see cref="GetOriginalVersions"/>.
        /// </param>
        /// <returns>Should return null or a version name with the mapped name.</returns>
        VersionedName OnVersionNotFound( IVersionedItem item, Func<string, VersionedTypedName> originalVersions );

        /// <summary>
        /// Called by the engine when a previous version is not found for the item
        /// This is an optional hook.
        /// </summary>
        /// <param name="item">Item for which a version should be found.</param>
        /// <param name="prevVersion">The not found previous version.</param>
        /// <param name="originalVersions">
        /// A getter for original versions. This can help the implementation to avoid duplicating its own version
        /// of <see cref="GetOriginalVersions"/>.
        /// </param>
        /// <returns>Should return null or a version name with the mapped name.</returns>
        VersionedName OnPreviousVersionNotFound( IVersionedItem item, VersionedName prevVersion, Func<string, VersionedTypedName> originalVersions );
    }
}
