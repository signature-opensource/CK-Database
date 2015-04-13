#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObj\Attribute\InjectContractAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Defines that an ambient contract must be injected: properties tagged with this attribute must 
    /// be <see cref="IAmbientContract"/> objects and are automatically injected.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class InjectContractAttribute : Attribute, IAmbientPropertyOrInjectContractAttribute
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

        bool IAmbientPropertyOrInjectContractAttribute.IsOptionalDefined
        {
            get { return _isOptional.HasValue; }
        }

        bool IAmbientPropertyOrInjectContractAttribute.IsAmbientProperty
        {
            get { return false; }
        }
    }
}
