using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public interface IContextualStObjMapRuntime : IContextualStObjMap
    {
        new IStObjRuntime ToLeaf( Type t );

        IStObjRuntime ToStObj( Type t );
    }

}
