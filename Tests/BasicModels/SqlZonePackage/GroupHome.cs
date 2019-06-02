using System.Data.SqlClient;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{
    [SqlTable( "tGroup", Package = typeof( Package ), ResourcePath=".Group" ), Versions( "CK.tGroup-Zone=2.11.25, 2.12.10" )]
    [SqlObjectItem( "transform:a_stupid_view" )]
    [SqlObjectItem( "zefuncNewGroup" )]
   public abstract class GroupHome : SqlActorPackage.Basic.GroupHome
    {
        void StObjConstruct( SecurityZoneHome zone )
        {
        }
        
        /// <summary>
        /// Finds or creates a Group. 
        /// </summary>
        /// <param name="securityZoneId">SecurityZone identifier of the group. Defaults to 0.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="groupIdResult">Group identifier.</param>
        /// <returns>The sql command object.</returns>
        [SqlProcedureNoExecute( "replace:sGroupCreate" )]
        public abstract SqlCommand CmdCreate( int securityZoneId, string groupName, out int groupIdResult );
        
        /// <summary>
        /// Can out parameters be optional? Yes, if they have a default value or are purely output (ie. not tagged with /*input*/ comment).
        /// </summary>
        /// <param name="c">The sql command that will be created or configured.</param>
        /// <param name="securityZoneId">SecurityZone identifier of the group. Defaults to 0.</param>
        /// <param name="groupName">Name of the group.</param>
        [SqlProcedureNoExecute( "replace:sGroupCreate" )]
        [SqlAlterProcedure]
        public abstract void CmdDemoCreate( ref SqlCommand c, int securityZoneId, string groupName );
    }
}
