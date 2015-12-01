#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\IContextualStObjMap.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Core
{
    /// <summary>
    /// Extends <see cref="IContextualTypeMap"/> to expose <see cref="IStObj"/> and Type to Object resolution.
    /// </summary>
    public interface IContextualStObjMap : IContextualTypeMap
    {
        /// <summary>
        /// Gets the most specialized <see cref="IStObj"/> or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type.</param>
        /// <returns>Most specialized StObj or null if no mapping exists for this type.</returns>
        IStObj ToLeaf( Type t );

        /// <summary>
        /// Gets the structured object or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type (that must be an Ambient Contract).</param>
        /// <returns>Structured object instance or null if the type has not been mapped.</returns>
        object Obtain( Type t );

        /// <summary>
        /// Access to all contexts.
        /// </summary>
        new IStObjMap AllContexts { get; }
    }
}
