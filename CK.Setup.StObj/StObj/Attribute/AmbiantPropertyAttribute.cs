using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Property, AllowMultiple=false, Inherited=true )]
    public class AmbiantPropertyAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether resolving this property is required or not.
        /// Defaults to false: unless expcitely stated, an ambiant property MUST be resolved.
        /// </summary>
        public bool IsOptional { get; set; }
    }
}
