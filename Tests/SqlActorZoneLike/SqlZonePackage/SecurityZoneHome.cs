using CK.Core;

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
