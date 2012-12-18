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
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class AmbientPropertyAttribute : Attribute, IAmbientPropertyOrContractAttribute
    {
        bool? _isOptional;

        /// <summary>
        /// Gets or sets whether resolving this property is required or not.
        /// Defaults to false (unless explicitely stated, an ambient property MUST be resolved) but when 
        /// is not explicitely set to true or false on a specialized property its value is given by property 
        /// definition of the base class. 
        /// </summary>
        public bool IsOptional
        {
            get { return _isOptional.HasValue ? _isOptional.Value : false; }
            set { _isOptional = value; }
        }

        bool IAmbientPropertyOrContractAttribute.IsOptionalDefined
        {
            get { return _isOptional.HasValue; }
        }

        bool IAmbientPropertyOrContractAttribute.IsAmbientProperty
        {
            get { return true; }
        }
    }
}
