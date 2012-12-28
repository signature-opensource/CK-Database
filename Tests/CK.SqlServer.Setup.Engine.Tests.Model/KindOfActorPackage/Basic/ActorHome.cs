using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tActor", Package = typeof( Package ) ), Versions( "CK.tActor=2.12.9, 2.12.10" )]
    [SqlObjectItem( "sActorCreate" )]
    public class ActorHome : SqlTable
    {
    }
}
