using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tUser", Package = typeof( Package ) ), Versions( "CK.tUser=2.12.9, 2.12.10" )]
    public abstract class UserHome : SqlTable
    {
        void Construct( ActorHome actor )
        {
        }

        [SqlProcedure( "sUserCreate" )]
        public abstract int Create( string userName );

    }
}
