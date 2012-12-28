using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{
    [SqlTable( "tSecurityZone", Package = typeof(Package) ), Versions( "CK.tSecurityZone=2.11.25, 2.12.10" ) ]
    [SqlObjectItem( "CKCore.sSecurityZoneSPInCKCoreSchema, sSecurityZoneCreate" )]
    public class SecurityZoneHome : SqlTable
    {
        void Construct( SqlActorPackage.Basic.GroupHome group )
        {
        }
    }
}
