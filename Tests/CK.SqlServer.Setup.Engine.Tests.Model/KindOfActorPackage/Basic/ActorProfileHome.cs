using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tActorProfile", Package = typeof( Package ) ), Versions( "CK.tActorProfile=2.12.9, 2.12.10" )]
    [SqlObjectItem( "fUserIsInGroup" )]
    public class ActorProfileHome : SqlTable
    {
        void Construct( ActorHome actor, GroupHome group )
        {
        }
    }
}
