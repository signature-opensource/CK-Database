#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlObject\ISqlObjectParser.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.Setup
{
    /// <summary>
    /// Abstraction of sql parser.
    /// This is not Sql Server specific.
    /// </summary>
    public interface ISqlObjectParser
    {
        IDependentProtoItem Create( IActivityMonitor monitor, IContextLocNaming externalName, string text );
    }
}
