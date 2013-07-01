using System;
using System.Collections.Generic;

namespace CK.Setup
{

    /// <summary>
    /// Handles persistent storage of versionning information.
    /// </summary>
    public interface IVersionedItemRepository
    {
        /// <summary>
        /// Gets the current <see cref="VersionedName"/> for a given <see cref="IVersionedItem"/>.
        /// </summary>
        /// <param name="i">The versionned item.</param>
        /// <returns>The versionned name that contains the current name and the current version of the item.</returns>
        VersionedName GetCurrent( IVersionedItem i );
        
        /// <summary>
        /// Updates the current version.
        /// </summary>
        /// <param name="i">The versioned item to update.</param>
        void SetCurrent( IVersionedItem i );

        /// <summary>
        /// Deletes the given item from the repository.
        /// Version is not required here: the item with the provided name will 
        /// be deleted regardless of its version.
        /// </summary>
        /// <param name="fullName">The <see cref="IDependentItem.FullName">FullName</see> to remove.</param>
        void Delete( string fullName );

    }
}
