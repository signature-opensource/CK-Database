#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\SqlZonePackage\SecurityZoneHome.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{
    [SqlTable( "tSecurityZone", Package = typeof(Package) ), Versions( "CK.tSecurityZone=2.11.25, 2.12.10" ) ]
    [SqlObjectItem( "CKCore.sSecurityZoneSPInCKCoreSchema, sSecurityZoneCreate" )]
    public class SecurityZoneHome : SqlTable, SqlActorPackage.ISecurityZoneAbstraction
    {
        bool SqlActorPackage.ISecurityZoneAbstraction.IAmHere() => true;

        void StObjConstruct( SqlActorPackage.Basic.GroupHome group )
        {
        }
    }
}
