using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RemoveDefaultContextAttribute : RemoveContextAttribute
    {
        public RemoveDefaultContextAttribute()
            : base( String.Empty )
        {
        }
    }
}
