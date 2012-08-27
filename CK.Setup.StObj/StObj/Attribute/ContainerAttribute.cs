using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Parameter, Inherited=false, AllowMultiple=false )]
    public class ContainerAttribute : Attribute
    {
        public ContainerAttribute()
        {
        }
    }
}
