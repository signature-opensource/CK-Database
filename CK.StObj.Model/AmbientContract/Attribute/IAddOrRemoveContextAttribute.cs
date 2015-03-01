#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\AmbientContract\Attribute\IAddOrRemoveContextAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Interface that unifies <see cref="AddContextAttribute"/> and <see cref="RemoveContextAttribute"/>.
    /// </summary>
    public interface IAddOrRemoveContextAttribute
    {
        string Context { get; }
    }
}
