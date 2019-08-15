using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Setup
{
    /// <summary>
    /// Captures the result of the <see cref="IVersionedItemReader.GetOriginalVersions(IActivityMonitor)"/>.
    /// </summary>
    public readonly struct OriginalReadInfo
    {
        /// <summary>
        /// Initializes a new <see cref="OriginalReadInfo"/>.
        /// </summary>
        /// <param name="result">The items. Must not be null.</param>
        /// <param name="fResult">The features. Must not be null.</param>
        public OriginalReadInfo( IReadOnlyCollection<VersionedTypedName> result, IReadOnlyCollection<VFeature> fResult ) : this()
        {
            Items = result ?? throw new ArgumentNullException( nameof( result ) );
            Features = fResult ?? throw new ArgumentNullException( nameof( fResult ) );
        }

        /// <summary>
        /// The versioned items.
        /// </summary>
        public IReadOnlyCollection<VersionedTypedName> Items { get; }

        /// <summary>
        /// The already registered features.
        /// </summary>
        public IReadOnlyCollection<VFeature> Features { get; }

    }
}
