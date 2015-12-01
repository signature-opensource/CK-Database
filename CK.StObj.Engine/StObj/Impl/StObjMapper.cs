#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\StObjMapper.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{
    internal class StObjMapper : AmbientTypeMap<StObjContextualMapper>
    {
        internal StObjMapper()
        {
        }

        protected override IContextualTypeMap CreateContext<T, TC>( IActivityMonitor monitor, string context )
        {
            return new StObjContextualMapper( this, context );
        }

    }
}
