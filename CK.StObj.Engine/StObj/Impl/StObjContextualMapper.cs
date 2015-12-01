#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\StObjContextualMapper.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    internal class StObjContextualMapper : AmbientContextualTypeMap<StObjTypeInfo, MutableItem>, IContextualStObjMapRuntime
    {
        internal StObjContextualMapper( StObjMapper owner, string context )
            : base( owner, context )
        {
        }

        IStObj IContextualStObjMap.ToLeaf( Type t )
        {
            return (IStObj)base.ToLeaf( t );
        }

        IStObjMap IContextualStObjMap.AllContexts
        {
            get { return (IStObjMap)base.AllContexts; }
        }


        IStObjResult IContextualStObjMapRuntime.ToLeaf( Type t )
        {
            return (IStObjResult)base.ToLeaf( t );
        }

        public object Obtain( Type t )
        {
            IStObjResult m = ToLeaf( t );
            return m != null ? m.ObjectAccessor() : null;
        }
        
        IStObjResult IContextualStObjMapRuntime.ToStObj( Type t )
        {
            return (IStObjResult)base.ToHighestImpl( t );
        }

    }
}
