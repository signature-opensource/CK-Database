#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\SqlZonePackage\Package.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{

    [SqlPackage( ResourceType = typeof( Package ), ResourcePath = "~SqlZonePackage.Res" ), Versions( "2.11.25" )]
    public abstract class Package : SqlActorPackage.Basic.Package
    {
        [InjectContract]
        public new GroupHome GroupHome { get { return (GroupHome)base.GroupHome; } protected set { base.GroupHome = value; } }

        [InjectContract]
        public SecurityZoneHome SecurityZoneHome { get; protected set; }

    }
}
