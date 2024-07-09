#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\KindOfActorPackage\Basic\Package.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using CK.Core;

namespace SqlActorPackage.SpecialItemType
{

    [SqlPackage( Schema = "CK", Database = typeof( SqlDefaultDatabase ), ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    public abstract class Package : SqlPackage
    {
    }
}
