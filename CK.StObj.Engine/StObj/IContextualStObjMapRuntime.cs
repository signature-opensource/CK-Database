#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\IContextualStObjMapRuntime.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public interface IContextualStObjMapRuntime : IContextualStObjMap
    {
        new IStObjResult ToLeaf( Type t );

        IStObjResult ToStObj( Type t );
    }

}
