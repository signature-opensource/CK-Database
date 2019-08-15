using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlZonePackage
{
    [SqlPackage( ResourcePath = "~SqlZonePackage.Res" ), Versions( "1.0.0" )]
    public abstract class PackageWithoutSchema : SqlPackage
    {
    }
}
