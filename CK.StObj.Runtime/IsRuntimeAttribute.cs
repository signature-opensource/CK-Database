using System;
using System.Collections.Generic;
using System.Text;


namespace CK.Setup
{

    /// <summary>
    /// Marks an assembly as being a Runtime. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
    public class IsRuntimeAttribute : Attribute
    {
    }
}
