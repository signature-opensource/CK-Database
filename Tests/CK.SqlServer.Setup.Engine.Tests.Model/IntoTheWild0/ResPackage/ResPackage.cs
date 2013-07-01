using CK.Setup;
using CK.SqlServer.Setup;

namespace IntoTheWild0
{
    [SqlPackage( Schema = "CK", HasModel=true, Database = typeof(SqlDefaultDatabase), ResourceType = typeof(ResPackage), ResourcePath="ResPackage.Resource" ), Versions( "2.9.2" )]
    public class ResPackage : SqlPackage
    {
    }

}
