using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Parameter, Inherited = false, AllowMultiple = false )]
    public class ContextAttribute : Attribute
    {
        public ContextAttribute( Type typedContext )
        {
            Context = typedContext;
        }

        public Type Context { get; private set; }
    }
}
