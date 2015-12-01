#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\IContextualTypeMap.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Exposes a contextual type to type mapping.
    /// </summary>
    public interface IContextualTypeMap
    {
        /// <summary>
        /// Gets the name of the context.
        /// </summary>
        string Context { get; }

        /// <summary>
        /// Gets the number of type mapped.
        /// </summary>
        int MappedTypeCount { get; }

        /// <summary>
        /// Gets the mapped type or null if no mapping exists.
        /// </summary>
        /// <param name="t">Key type.</param>
        /// <returns>Mapped type or null if no mapping exists for this type.</returns>
        Type ToLeafType( Type t );

        /// <summary>
        /// Gets whether a type is mapped.
        /// </summary>
        /// <param name="t">Type to lookup.</param>
        /// <returns>True if <paramref name="t"/> is mapped in this context, false otherwise.</returns>
        bool IsMapped( Type t );

        /// <summary>
        /// Gets all types mapped by this contextual map.
        /// </summary>
        IEnumerable<Type> Types { get; }

        /// <summary>
        /// Access to all contexts.
        /// </summary>
        IContextualRoot<IContextualTypeMap> AllContexts { get; }
    }
}
