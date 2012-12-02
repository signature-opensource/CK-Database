using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Defines an ambient property: properties tagged with this attribute can be automatically set
    /// with identically named properties value from containers.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple=false, Inherited=true )]
    public class AmbientPropertyAttribute : Attribute
    {
        bool? _isOptional;

        /// <summary>
        /// Gets or sets whether resolving this property is required or not.
        /// Defaults to false: unless expcitely stated, an ambient property MUST be resolved.
        /// When not set on a specialized property it is driven by property definition of the base class. 
        /// </summary>
        public bool IsOptional 
        {
            get { return _isOptional.HasValue ? _isOptional.Value : false; }
            set { _isOptional = value; } 
        }

        internal bool IsOptionalDefined { get { return _isOptional.HasValue; } }
    }
}
