using CK.SqlServer.Setup;

namespace SqlActorPackage
{

    [SqlPackage( ResourcePath = "TransformPackageRes", Schema = "CK", Database = typeof( SqlDefaultDatabase ) )]
    [SqlObjectItem( "transform:sGroupDestroy" )]
    public class TransformPackageSample : SqlPackage
    {
        void StObjConstruct( SqlActorPackage.Basic.Package actorPackage )
        {
        }
    }
}
