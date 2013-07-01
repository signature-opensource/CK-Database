using System.Data.SqlClient;
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
        public abstract SqlCommand CmdCreate( string userName, out int userIdResult );

        [SqlProcedure( "sUserExists" )]
        public abstract void CmdExists( ref SqlCommand cmdExists, string userName, out bool existsResult );

        [SqlProcedure( "sUserExists2" )]
        public abstract void CmdExists2( ref SqlCommand cmdExists, int userPart1, int userPart2, out bool existsResult );

    }
}
