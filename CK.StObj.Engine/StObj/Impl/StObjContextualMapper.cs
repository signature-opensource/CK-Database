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


        IStObjRuntime IContextualStObjMapRuntime.ToLeaf( Type t )
        {
            return (IStObjRuntime)base.ToLeaf( t );
        }

        public object Obtain( Type t )
        {
            IStObjRuntime m = ToLeaf( t );
            return m != null ? m.Object : null;
        }
        
        IStObjRuntime IContextualStObjMapRuntime.ToStObj( Type t )
        {
            return (IStObjRuntime)base.ToHighestImpl( t );
        }

    }
}
