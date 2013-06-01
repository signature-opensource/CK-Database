using CK.Setup;
using CK.SqlServer.Setup;

namespace IntoTheWild0
{

    [SqlTable( "tResDataRawText", Package = typeof( ResPackage ) ), Versions( "2.9.2" )]
    public class ResDataRawTextHome : SqlTable
    {
        void Construct( ResHome res )
        {
        }
    }

}
