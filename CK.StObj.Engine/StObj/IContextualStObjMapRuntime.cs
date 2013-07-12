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
