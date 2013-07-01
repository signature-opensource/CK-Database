using CK.Setup;
using CK.SqlServer.Setup;

namespace IntoTheWild0
{

    [SqlTable( "tResDataString", Package = typeof( ResPackage ) ), Versions( "2.9.2" )]
    [SqlObjectItem( "sResDataStringSet" )]
    [SqlObjectItem( "sResDataStringRemove" )]
    public class ResDataStringHome : SqlTable
    {
        void Construct( ResHome res )
        {
        }
    }
}
