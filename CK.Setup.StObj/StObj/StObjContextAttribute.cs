using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Parameter )]
    public class StObjContextAttribute : Attribute
    {
        public StObjContextAttribute( Type typedContext )
        {
            Context = typedContext;
        }

        public Type Context { get; private set; }
    }
}
