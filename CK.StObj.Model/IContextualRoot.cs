#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\IContextualRoot.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Exposes a multi contextual type mapping.
    /// </summary>
    public interface IContextualRoot<out T>
    {
        /// <summary>
        /// Gets the default contextual information, the one identified by <see cref="String.Empty"/>.
        /// </summary>
        T Default { get; }

        /// <summary>
        /// Gets the different contexts (including <see cref="Default"/>).
        /// </summary>
        IReadOnlyCollection<T> Contexts { get; }

        /// <summary>
        /// Gets the contextualStObj map or null if context is unknown.
        /// </summary>
        /// <param name="context">Context name.</param>
        /// <returns>Contextual mapping or null if no such context exists.</returns>
        T FindContext( string context );
    }
}
