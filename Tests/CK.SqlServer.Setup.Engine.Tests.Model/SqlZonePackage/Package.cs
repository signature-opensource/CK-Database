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
