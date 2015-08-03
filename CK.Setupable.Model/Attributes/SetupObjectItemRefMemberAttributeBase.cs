#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\Attributes\SqlMethodForObjectItemAttributeBase.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection.Emit;
using System.Reflection;

namespace CK.Setup
{
    /// <summary>
    /// Base class for an attribute appleid to a member that is associated to a SetupObjectItem that must be defined 
    /// by another attribute on the same member (typically a <see cref="SetupObjectItemMemberAttributeBase"/>).
    /// </summary>
    public abstract class SetupObjectItemRefMemberAttributeBase : AmbientContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes this attribute with the assembly qualified name of actual implementation.
        /// </summary>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
        protected SetupObjectItemRefMemberAttributeBase( string actualAttributeTypeAssemblyQualifiedName )
            : base( actualAttributeTypeAssemblyQualifiedName )
        {
        }

    }
}
