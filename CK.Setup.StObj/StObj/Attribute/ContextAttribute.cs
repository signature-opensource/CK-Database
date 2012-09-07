using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Offers a way to statically bind a parameter or a property to a typed context.
    /// </summary>
    /// <remarks>
    /// The <see cref="AttributeTargets.ReturnValue"/> is defined here but is currently not used by the framework.
    /// </remarks>
    [AttributeUsage( AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, Inherited = false, AllowMultiple = false )]
    public class ContextAttribute : Attribute
    {
        public ContextAttribute( Type typedContext )
        {
            Context = typedContext;
        }

        public Type Context { get; private set; }
    }
}
