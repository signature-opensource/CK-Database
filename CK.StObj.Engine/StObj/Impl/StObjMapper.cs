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

        //IContextualStObjMap IStObjMapper.Default
        //{
        //    get { return base.Default; }
        //}

        //IReadOnlyCollection<IContextualStObjMap> IStObjMapper.Contexts 
        //{
        //    get { return base.Contexts; } 
        //}

        //IContextualStObjMap IStObjMapper.FindContext( string context )
        //{
        //    return base.FindContext( context );
        //}

        protected override IAmbientContextualTypeMap CreateContext<T, TC>( IActivityLogger logger, string context )
        {
            return new StObjContextualMapper( this, context );
        }

    }
}
