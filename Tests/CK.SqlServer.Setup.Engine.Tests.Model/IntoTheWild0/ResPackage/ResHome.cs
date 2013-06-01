using CK.Setup;
using CK.SqlServer.Setup;

namespace IntoTheWild0
{
    [SqlTable( "tRes", Package = typeof( ResPackage ) ), Versions( "2.9.2" )]
    [SqlObjectItem( "sResCreate" )]
    [SqlObjectItem( "sResRemove" )]
    public class ResHome : SqlTable
    {
    }
}
