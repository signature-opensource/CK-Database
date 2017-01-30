using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{

    [SqlPackage( ResourceType = typeof( Package ), ResourcePath = "~SqlZonePackage.Res" ), Versions( "2.11.25" )]
    [SqlObjectItem( "replace:sUserToBeOverridenIndirect" )]
    [SqlActorPackage.TestAutoHeaderSP( "Injected from SqlZonePackage.Zone.Package.TestAutoHeaderSP attribute (n°2/2).", "sUserToBeOverridenIndirect" )]
    public abstract class Package : SqlActorPackage.Basic.Package
    {
        [InjectContract]
        public new GroupHome GroupHome { get { return (GroupHome)base.GroupHome; } protected set { base.GroupHome = value; } }

        [InjectContract]
        public SecurityZoneHome SecurityZoneHome { get; protected set; }

    }
}
