#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Version\IVersionedItemRepository.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Setup
{

    /// <summary>
    /// Writes versionning information of a system.
    /// </summary>
    public interface IVersionedItemWriter
    {
        /// <summary>
        /// Must updates the version informations or throws an exception if anything prevents the versions
        /// to be correctly updated.
        /// When <paramref name="deleteUnaccessedItems"/> is true, any non accessed names can be removed (if they exist)
        /// otherwise only names that has been deleted or have a new version should be updated.
        /// </summary>
        /// <param name="monitor">Monitor to use for warnings or informations. Exceptions should be thrown an any serious error.</param>
        /// <param name="reader">
        /// The reader that has been used to read the original versions: this can be used to enable 
        /// checks and/or optimizations.
        /// </param>
        /// <param name="trackedItems">The set of <see cref="VersionedNameTracked"/> objects.</param>
        /// <param name="deleteUnaccessedItems">True to delete unaccessed items.</param>
        void SetVersions( IActivityMonitor monitor, IVersionedItemReader reader, IEnumerable<VersionedNameTracked> trackedItems, bool deleteUnaccessedItems );
    }
}
