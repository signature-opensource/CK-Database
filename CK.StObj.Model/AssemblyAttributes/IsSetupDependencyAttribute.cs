using System;
using System.Collections.Generic;
using System.Text;


namespace CK.Setup
{
    /// <summary>
    /// Marks an assembly as being a setup dependency.
    /// A setup dependency can have <see cref="RequiredSetupDependencyAttribute"/> just like Models.
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
    public class IsSetupDependencyAttribute : Attribute
    {
    }
}
