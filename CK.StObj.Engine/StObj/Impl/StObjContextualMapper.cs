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

        IStObj IContextualStObjMap.ToLeaf( Type t ) => (IStObj)base.ToLeaf( t );

        IStObjMap IContextualStObjMap.AllContexts =>  (IStObjMap)base.AllContexts;

        IEnumerable<object> IContextualStObjMap.Implementations => throw new NotImplementedException( "This internal is not used on the Engine." );

        IEnumerable<StObjImplementation> IContextualStObjMap.StObjs => throw new NotImplementedException("This internal is not used on the Engine.");

        IEnumerable<KeyValuePair<Type, object>> IContextualStObjMap.Mappings => throw new NotImplementedException("This internal is not used on the Engine.");

        IStObjResult IContextualStObjMapRuntime.ToLeaf( Type t ) => (IStObjResult)base.ToLeaf( t );

        public object Obtain(Type t) => ToLeaf(t)?.InitialObject;
        
        IStObjResult IContextualStObjMapRuntime.ToStObj( Type t ) => (IStObjResult)base.ToHighestImpl( t );

    }
}
