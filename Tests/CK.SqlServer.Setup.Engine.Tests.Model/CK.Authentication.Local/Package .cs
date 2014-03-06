using CK.Setup;
using CK.SqlServer.Setup;

namespace CK.Authentication.Local
{
    [SqlPackage( Database = typeof(SqlDefaultDatabase), Schema="CK", ResourceType = typeof( Package ), ResourcePath = "Res" ), Versions( "1.0.0" )]
    public class Package : SqlPackage
    {
        [InjectContract]
        public UserHome UserHome { get; protected set; }
    }
}
