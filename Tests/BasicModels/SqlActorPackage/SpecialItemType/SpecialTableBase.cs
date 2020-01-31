using CK.Core;

namespace SqlActorPackage.SpecialItemType
{
    [CKTypeDefiner]
    [Setup( ItemTypeName = "SqlActorPackage.Runtime.SpecialTableBaseItem, SqlActorPackage.Runtime" )]
    public class SpecialTableBase : SqlTable
    {
        void StObjConstruct()
        {
        }
    }
}
