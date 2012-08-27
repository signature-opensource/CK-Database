using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public interface IStObjDependencyResolver
    {
        object Resolve( IActivityLogger logger, IParameterType parameter );
    }

}
