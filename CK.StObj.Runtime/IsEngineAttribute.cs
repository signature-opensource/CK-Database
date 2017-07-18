using System;
using System.Collections.Generic;
using System.Text;


namespace CK.Setup
{

    /// <summary>
    /// Marks an assembly as an Engine. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
    public class IsEngineAttribute : Attribute
    {
    }
}
