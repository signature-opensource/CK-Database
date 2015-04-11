#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\IntoTheWild0\ResPackage\ResPackage.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using CK.Setup;
using CK.SqlServer.Setup;

namespace IntoTheWild0
{
    [SqlPackage( Schema = "CK", Database = typeof( SqlDefaultDatabase ), ResourceType = typeof( ResPackage ), ResourcePath = "ResPackage.Resource" ), Versions( "2.9.2" )]
    public class ResPackage : SqlPackage
    {
    }
}
