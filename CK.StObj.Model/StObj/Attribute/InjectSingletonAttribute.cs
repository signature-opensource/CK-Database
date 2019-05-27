using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Defines that an ambient singleton must be injected: properties tagged with this attribute must 
    /// be <see cref="IAmbientObject"/> objects or <see cref="IAmbientService"/> that will be considered
    /// as <see cref="ISingletonAmbientService"/> and are automatically injected.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class InjectSingletonAttribute : Attribute, IAmbientPropertyOrInjectSingletonAttribute
    {
        bool? _isOptional;

        /// <summary>
        /// Gets or sets whether finding the corresponding typed singleton is required or not.
        /// Defaults to false (unless explicitly stated, the type must be resolved) but when 
        /// is not explicitly set to true or false on a specialized property its value is given by property 
        /// definition of the base class. 
        /// </summary>
        public bool IsOptional
        {
            get { return _isOptional.HasValue ? _isOptional.Value : false; }
            set { _isOptional = value; }
        }

        bool IAmbientPropertyOrInjectSingletonAttribute.IsOptionalDefined => _isOptional.HasValue; 

        bool IAmbientPropertyOrInjectSingletonAttribute.IsAmbientProperty => false; 
    }
}
