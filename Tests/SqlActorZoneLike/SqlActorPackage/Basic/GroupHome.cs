using Microsoft.Data.SqlClient;
using CK.Core;
using CK.SqlServer;

namespace SqlActorPackage.Basic;

[SqlTable( "tGroup", Package = typeof( Package ) ), Versions( "CK.tGroup=2.12.9, 2.12.10" )]
[SqlObjectItem( "a_stupid_view" )]
public abstract class GroupHome : SqlTable, IAnyService
{
    void StObjConstruct( ActorHome actor )
    {
    }

    /// <summary>
    /// Creates a command to call sDestroyGroup.
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns>The sql command object.</returns>
    [SqlProcedureNoExecute( "sGroupDestroy" )]
    public abstract SqlCommand CmdDestroy( int groupId );

    /// <summary>
    /// Finds or creates a Group. 
    /// </summary>
    /// <param name="groupName">Name of the group.</param>
    /// <param name="groupIdResult">Group identifier.</param>
    [SqlProcedure( "sGroupCreate" )]
    public abstract void CmdCreate( ISqlCallContext ctx, string groupName, out int groupIdResult );

    public virtual string CallService()
    {
        return GetType().FullName;
    }
}
