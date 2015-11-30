#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Runtime\IStObjMutableInjectAmbientContract.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Describes a <see cref="IStObjMutableReference"/> that is an injected Ambient Contract: such references are defined by properties 
    /// marked with <see cref="InjectContractAttribute"/>. The property type is necessarily a <see cref="IAmbientContract"/> and
    /// typically use covariance between StObj layers. 
    /// </summary>
    public interface IStObjMutableInjectAmbientContract : IStObjMutableReference
    {
        /// <summary>
        /// Gets the name of the Ambient Contract property.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether the resolution of this property is optional.
        /// When it is true (see remarks) and the resolution fails, the property will not be set.
        /// </summary>
        /// <remarks>
        /// If this is true, it means that all property definition across the inheritance chain has [<see cref="InjectContractAttribute">InjectAmbientContract</see>( <see cref="AmbientContractAttribute.IsOptional">IsOptional</see> = true ]
        /// attribute (from the most abstract property definition), because a required property can NOT become optional.
        /// (Note that the reverse is not true: an optional ambient property can perfectly be made required by Specializations.)
        /// </remarks>
        bool IsOptional { get; }

    }
}
