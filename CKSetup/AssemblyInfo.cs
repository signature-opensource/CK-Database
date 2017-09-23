
using System;


[assembly: CK.Setup.IsSetupDependency()]

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
    class IsSetupDependencyAttribute : Attribute
    {
    }
}

