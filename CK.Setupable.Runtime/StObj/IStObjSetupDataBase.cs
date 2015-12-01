#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\StObj\IStObjSetupDataBase.cs) is part of CK-Database. 
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
    public interface IStObjSetupDataBase
    {
        /// <summary>
        /// Gets the parent setup data if it exists (this is to manage attribute properties "inheritance"). 
        /// Null if this object corresponds to the first (root) <see cref="IAmbientContract"/> of the inheritance chain.
        /// </summary>
        IStObjSetupData Generalization { get; }

        /// <summary>
        /// Gets the associated <see cref="IStObjResult"/>.
        /// Never null.
        /// </summary>
        IStObjResult StObj { get; }

        /// <summary>
        /// Gets the [contextualized] full name of the object.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets whether the <see cref="FullName"/> is the default one (default full name is the <see cref="IStObj.ObjectType">StObj.ObjectType</see>.<see cref="Type.FullName">FullName</see>).
        /// </summary>
        bool IsDefaultFullNameWithoutContext { get; }
    }
}
