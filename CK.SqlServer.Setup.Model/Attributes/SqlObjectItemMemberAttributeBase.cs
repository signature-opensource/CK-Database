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
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Base class for <see cref="SqlProcedureNoExecuteAttribute"/>.
    /// </summary>
    public abstract class SqlObjectItemMemberAttributeBase : SetupObjectItemMemberAttributeBase
    {
        /// <summary>
        /// Initializes this attribute this a name (procedure name like "sUserCreate")
        /// </summary>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
        protected SqlObjectItemMemberAttributeBase( string objectName, string actualAttributeTypeAssemblyQualifiedName )
            : base( objectName, actualAttributeTypeAssemblyQualifiedName )
        {
        }
    }
}
