using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RequiredByAttribute : RequiresAttribute
    {
        /// <summary>
        /// Defines requirements by their names.
        /// </summary>
        /// <param name="requires">Comma separated list of requirement item names.</param>
        public RequiredByAttribute( string requires )
            : base( requires )
        {            
        }

    }
}
