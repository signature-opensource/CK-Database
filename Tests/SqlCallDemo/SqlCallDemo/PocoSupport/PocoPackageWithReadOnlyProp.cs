using CK.Core;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res.Poco" ), Versions( "1.0.0" )]
    [SqlObjectItem( "transform:sPocoThingRead" )]
    public class PocoPackageWithReadOnlyProp : SqlPackage
    {
        void StObjConstruct( PocoPackage p )
        {
        }
    }
}
