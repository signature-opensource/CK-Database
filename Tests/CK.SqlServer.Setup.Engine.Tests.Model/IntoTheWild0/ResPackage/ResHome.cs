#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\IntoTheWild0\ResPackage\ResHome.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using CK.Setup;
using CK.SqlServer.Setup;

namespace IntoTheWild0
{
    [SqlTable( "tRes", Package = typeof( ResPackage ) ), Versions( "2.9.2" )]
    [SqlObjectItem( "sResCreate" )]
    [SqlObjectItem( "sResRemove" )]
    public class ResHome : SqlTable
    {
    }
}

namespace IntoTheWild0.Histo
{
    [RemoveDefaultContext()]
    [AddContext( "dbHisto" )]
    [SqlTable( "tRes", Package = typeof( ResPackage ) ), Versions( "2.9.2" )]
    [SqlObjectItem( "sResCreate" )]
    [SqlObjectItem( "sResRemove" )]
    public class ResHome : SqlTable
    {
    }
}
