#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObj\Attribute\AmbientPropertyAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Defines an ambient property: properties tagged with this attribute can be automatically set
    /// with identically named properties value from containers. The <see cref="ResolutionSource"/> is inherited from 
    /// the same property on the base class or defaults to <see cref="PropertyResolutionSource.FromGeneralizationAndThenContainer"/>.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class AmbientPropertyAttribute : Attribute, IAmbientPropertyOrInjectContractAttribute
    {
        bool? _isOptional;
        PropertyResolutionSource? _source;

        /// <summary>
        /// Initializes a new <see cref="AmbientPropertyAttribute"/>.
        /// </summary>
        public AmbientPropertyAttribute()
        {
            _source = PropertyResolutionSource.FromGeneralizationAndThenContainer;
        }

        /// <summary>
        /// Gets or sets whether resolving this property is required or not.
        /// Defaults to false (unless explicitly stated, an ambient property MUST be resolved) but when 
        /// is not explicitly set to true or false on a specialized property its value is given by property 
        /// definition of the base class. 
        /// </summary>
        public bool IsOptional
        {
            get { return _isOptional.HasValue ? _isOptional.Value : false; }
            set { _isOptional = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="PropertyResolutionSource"/> for this property.
        /// Defaults to <see cref="PropertyResolutionSource.FromGeneralizationAndThenContainer"/>, but when 
        /// it is not explicitly set, its value is inherited from the property definition of the base class. 
        /// </summary>
        public PropertyResolutionSource ResolutionSource
        {
            get { return _source.HasValue ? _source.Value : PropertyResolutionSource.FromGeneralizationAndThenContainer; }
            set { _source = value; }
        }

        /// <summary>
        /// Gets whether <see cref="ResolutionSource"/> has been set.
        /// </summary>
        public bool IsResolutionSourceDefined
        {
            get { return _source.HasValue; }
        }

        bool IAmbientPropertyOrInjectContractAttribute.IsOptionalDefined
        {
            get { return _isOptional.HasValue; }
        }

        bool IAmbientPropertyOrInjectContractAttribute.IsAmbientProperty
        {
            get { return true; }
        }
    }
}
