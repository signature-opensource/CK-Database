using CK.Core;
using SqlActorPackage;

namespace SqlZonePackage.Zone;


[SqlPackage( ResourceType = typeof( Package ), ResourcePath = "~SqlZonePackage.Res" ), Versions( "2.11.25" )]
[SqlObjectItem( "replace:sUserToBeOverridenIndirect" )]
[SqlActorPackage.TestAutoHeaderSP( "Injected from SqlZonePackage.Zone.Package.TestAutoHeaderSP attribute (n°2/2).", "sUserToBeOverridenIndirect" )]
public abstract class Package : SqlActorPackage.Basic.Package, SqlActorPackage.IAnyService
{
    [InjectObject]
    public new GroupHome GroupHome { get { return (GroupHome)base.GroupHome; } }

    [InjectObject]
    public SecurityZoneHome SecurityZoneHome { get; protected set; }

    string IAnyService.CallService() => "ZonePackage!!";
}
