#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObj\Impl\StructuredObjectCache.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    class StructuredObjectCache
    {
        readonly object[] _cache;

        public StructuredObjectCache( int nbSpecializations )
        {
            _cache = new object[nbSpecializations];
        }

        public object Get( int idx )
        {
            return _cache[idx];
        }
        
        public void Set( int idx, object instance )
        {
            _cache[idx] = instance;
        }
    }
}
