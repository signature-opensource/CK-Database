using System.Data.SqlClient;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{
    [SqlTable( "tUser", Package = typeof( Package ) ), Versions( "25.03.30" )]
    public abstract class UserHome : SqlActorPackage.Basic.UserHome
    {
        [SqlProcedure( "sUserToBeOverriden" )]
        [SqlActorPackage.TestAutoHeaderSPMember( "Injected from SqlZonePackage.Zone.UserHome.CmdUserToBeOverriden (n°2/2)." )]
        public abstract void CmdUserToBeOverriden( ref SqlCommand cmdExists, int param1, int paramFromZone, out bool done );
        
        /// <summary>
        /// Can out parameters be optional? Yes, if they have a default value or are purely output (ie. not tagged with /*input*/ comment).
        /// </summary>
        /// <param name="c">The sql command that will be created or configured.</param>
        /// <param name="securityZoneId">SecurityZone identifier of the group. Defaults to 0.</param>
        /// <param name="groupName">Name of the group.</param>
        [SqlProcedure( "sGroupCreate" )]
        public abstract void CmdDemoCreate( ref SqlCommand c, int securityZoneId, string groupName );
    }
}
