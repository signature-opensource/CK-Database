using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface IStObjDependencyResolver
    {
        object Resolve( IMutableParameterType parameter );
    }

}
