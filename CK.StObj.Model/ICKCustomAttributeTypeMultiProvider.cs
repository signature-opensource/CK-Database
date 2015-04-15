#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Runtime\ICustomAttributeMultiProvider.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Specialized <see cref="ICKCustomAttributeMultiProvider"/> bound to a <see cref="P:Type"/>. 
    /// Attributes of the Type itself MUST be available from this interface.
    /// </summary>
    public interface ICKCustomAttributeTypeMultiProvider : ICKCustomAttributeMultiProvider
    {
        /// <summary>
        /// Gets the type to which this provider is bound.
        /// The attributes of this type are available (recall that a Type is a MemberInfo).
        /// </summary>
        Type Type { get; }
    }
}
