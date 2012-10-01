using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Defines an ambient property: properties tagged with this attribute can be automatically set
    /// by properties from their containers.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple=false, Inherited=true )]
    public class AmbientPropertyAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether resolving this property is required or not.
        /// Defaults to false: unless expcitely stated, an ambient property MUST be resolved.
        /// </summary>
        public bool IsOptional { get; set; }
    }
}
