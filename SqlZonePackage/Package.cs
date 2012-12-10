using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;

namespace SqlZonePackage.Zone
{

    [SqlPackage( ResourceType = typeof( Package ), ResourcePath = "~SqlZonePackage.Res" ), Versions( "2.11.25" )]
    public class Package : SqlActorPackage.Basic.Package
    {
        [AmbientContract]
        public new GroupHome GroupHome { get { return (GroupHome)base.GroupHome; } protected set { base.GroupHome = value; } }

        [AmbientContract]
        public SecurityZoneHome SecurityZoneHome { get; protected set; }

    }
}
