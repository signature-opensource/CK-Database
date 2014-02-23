using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Defines an ambient contract property: properties tagged with this attribute must 
    /// be <see cref="IAmbientContract"/> objects and are automatically injected.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class AmbientContractAttribute : Attribute, IAmbientPropertyOrContractAttribute
    {
        bool? _isOptional;

        /// <summary>
        /// Gets or sets whether finding the corresponding typed <see cref="IAmbientContract"/> is required or not.
        /// Defaults to false (unless explicitly stated, the type must be resolved) but when 
        /// is not explicitly set to true or false on a specialized property its value is given by property 
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
            get { return false; }
        }
    }
}
