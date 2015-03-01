#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\SqlZonePackage\UserView.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{
    [SqlView( "vUser", Package = typeof( Zone.Package ) )]
    public class UserView : SqlActorPackage.Basic.UserView
    {
        void Construct( SecurityZoneHome zoneHome )
        {
        }
    }
}
