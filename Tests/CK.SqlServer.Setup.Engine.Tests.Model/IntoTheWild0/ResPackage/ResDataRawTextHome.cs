#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\IntoTheWild0\ResPackage\ResDataRawTextHome.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Setup;
using CK.SqlServer.Setup;

namespace IntoTheWild0
{

    [SqlTable( "tResDataRawText", Package = typeof( ResPackage ) ), Versions( "2.9.2" )]
    public class ResDataRawTextHome : SqlTable
    {
        void Construct( ResHome res )
        {
        }
    }

}
