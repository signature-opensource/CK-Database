using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{
    [SqlTable( "tGroup", Package = typeof( Package ) ), Versions( "CK.tGroup-Zone=2.11.25, 2.12.10" ) ]
    public class GroupHome : SqlActorPackage.Basic.GroupHome
    {
        void Construct( SecurityZoneHome zone )
        {
        }
    }
}
