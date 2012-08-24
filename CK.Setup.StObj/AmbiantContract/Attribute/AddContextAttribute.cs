using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class AddContextAttribute : Attribute, IContextDefiner
    {
        public AddContextAttribute( Type context )
        {
            Context = context;
        }

        public Type Context { get; private set; }
    }
}
