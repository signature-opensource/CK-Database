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

        protected override IContextualTypeMap CreateContext<T, TC>( IActivityLogger logger, string context )
        {
            return new StObjContextualMapper( this, context );
        }

    }
}
