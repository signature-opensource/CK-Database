using System;
using System.Collections.Generic;
using System.Text;


namespace CK.Setup
{

    /// <summary>
    /// Decorates an assembly with an associated engine assembly. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
    public class IsEngineAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new setup engine attribute.
        /// </summary>
        public IsEngineAttribute()
        {
        }

    }
}
