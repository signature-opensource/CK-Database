#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObj\Attribute\IAmbientPropertyOrInjectContractAttribute.cs) is part of CK-Database. 
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
    /// Unifies <see cref="AmbientPropertyAttribute"/> and <see cref="InjectContractAttribute"/>.
    /// </summary>
    public interface IAmbientPropertyOrInjectContractAttribute
    {
        /// <summary>
        /// Gets whether resolving this property is required or not.
        /// </summary>
        bool IsOptional { get; }

        /// <summary>
        /// Gets whether that attribute defines the <see cref="IsOptional"/> value or if it must be inherited.
        /// </summary>
        bool IsOptionalDefined { get; }

        /// <summary>
        /// Gets whether the property is an ambient property. Otherwise it is an injected contract.
        /// </summary>
        bool IsAmbientProperty { get; }
    }
}
