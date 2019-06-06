using System;
using CK.Setup;
using CK.SqlServer.Setup;
using SqlActorPackage;

namespace SqlZonePackage.Zone
{

    [SqlPackage( ResourceType = typeof( Package ), ResourcePath = "~SqlZonePackage.Res" ), Versions( "2.11.25" )]
    [SqlObjectItem( "replace:sUserToBeOverridenIndirect" )]
    [SqlActorPackage.TestAutoHeaderSP( "Injected from SqlZonePackage.Zone.Package.TestAutoHeaderSP attribute (n°2/2).", "sUserToBeOverridenIndirect" )]
    public abstract class Package : SqlActorPackage.Basic.Package, SqlActorPackage.IAnyService
    {
        [InjectObjectAttribute]
        public new GroupHome GroupHome { get { return (GroupHome)base.GroupHome; } }

        [InjectObjectAttribute]
        public SecurityZoneHome SecurityZoneHome { get; protected set; }

        string IAnyService.CallService() => "ZonePackage!!";
    }
}
