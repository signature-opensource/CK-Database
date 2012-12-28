using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tGroup", Package = typeof( Package ) ), Versions( "CK.tGroup=2.12.9, 2.12.10" )]
    [SqlObjectItem( "sGroupCreate" )]
    public class GroupHome : SqlTable
    {
        void Construct( ActorHome actor )
        {
        }
    }
}
