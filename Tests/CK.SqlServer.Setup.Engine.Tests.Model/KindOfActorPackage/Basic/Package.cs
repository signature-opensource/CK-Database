using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Basic
{

    [SqlPackage( Schema = "CK", Database = typeof( SqlDefaultDatabase ), ResourceType = typeof( Package ), ResourcePath = "Res" ), Versions( "2.11.25" )]
    public class Package : SqlPackage
    {
        [AmbientContract]
        public UserHome UserHome { get; protected set; }
        
        [AmbientContract]
        public GroupHome GroupHome { get; protected set; }
    }
}
