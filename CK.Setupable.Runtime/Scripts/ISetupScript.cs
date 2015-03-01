#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\ISetupScript.cs) is part of CK-Database. 
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
    /// A <see cref="ParsedFileName"/> associated to a way to read its content and a script source.
    /// </summary>
    /// <remarks>
    /// The <see cref="ScriptSource"/> drives the choice of the ScriptTypeHandler that will be used
    /// to execute the script.
    /// </remarks>
    public interface ISetupScript
    {
        ParsedFileName Name { get; }

        string ScriptSource { get; }

        string GetScript();
    }
}
