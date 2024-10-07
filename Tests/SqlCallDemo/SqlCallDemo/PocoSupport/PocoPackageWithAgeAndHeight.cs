using CK.Core;

namespace SqlCallDemo;


[SqlPackage( Schema = "CK", ResourcePath = "Res.Poco" ), Versions( "1.0.0" )]
[SqlObjectItem( "transform:sPocoThingWrite, transform:sPocoThingRead" )]
public class PocoPackageWithAgeAndHeight : SqlPackage
{
    void StObjConstruct( PocoPackage p )
    {
    }
}
