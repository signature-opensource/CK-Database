using CK.Core;

namespace SqlZonePackage
{
    [SqlPackage( ResourcePath = "~SqlZonePackage.Res" ), Versions( "1.0.0" )]
    public abstract class PackageWithoutSchema : SqlPackage
    {
    }
}
