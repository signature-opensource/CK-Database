using System;
using System.Collections.Generic;

namespace CK.Setup
{
    /// <summary>
    /// Defines the identity of a versionable <see cref="IDependentItem"/>.
    /// </summary>
    public interface IVersionedItem : IDependentItem
	{        
        /// <summary>
        /// Gets an identifier of the type of the item. This is required
        /// in order to be able to handle specific storage for version without 
        /// relying on any <see cref="FullName"/> conventions.
        /// </summary>
        string ItemType { get; }

        /// <summary>
        /// Gets current version. 
        /// Null if no version exists or applies to this object.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets an optionnal list of <see cref="VersionedName"/>.
        /// Can be null if no previous names exists.
        /// </summary>
        IEnumerable<VersionedName> PreviousNames { get; }
	}
}
