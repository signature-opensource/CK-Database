#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\Attributes\SqlMethodForObjectItemAttributeBase.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Base class for attributes that define a SetupObjectItem.
    /// </summary>
    public abstract class SetupObjectItemMemberAttributeBase : AmbientContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes this attribute with the name of the SetupItem (like "sUserCreate" or "CK.sUserCreate").
        /// </summary>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
        protected SetupObjectItemMemberAttributeBase( string objectName, string actualAttributeTypeAssemblyQualifiedName )
            : base( actualAttributeTypeAssemblyQualifiedName )
        {
            ObjectName = objectName;
        }

        /// <summary>
        /// Gets the object name (for instance "sUserCreate" or "CK.sUserCreate").
        /// </summary>
        public string ObjectName { get; private set; }

    }
}
