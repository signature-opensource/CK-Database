#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\IStObjMap.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;

namespace CK.Core
{

    /// <summary>
    /// Main interface that offers access to multi contextual type mapping and 
    /// Ambient Contract instantiation.
    /// </summary>
    public interface IStObjMap : IContextualRoot<IContextualStObjMap>
    {
        /// <summary>
        /// Gets all the mappings this StObjMap contains.
        /// </summary>
        IEnumerable<StObjMapMapping> AllMappings { get; }
    }
}
