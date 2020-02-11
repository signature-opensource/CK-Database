using CK.Core;

namespace SqlActorPackage.SpecialItemType
{
    [CKTypeDefiner]
    [Setup( ItemTypeName = "SqlActorPackage.Runtime.SpecialTableBaseItem, SqlActorPackage.Runtime" )]
    public class SpecialTableBase : SqlTable
    {
        void StObjConstruct( SqlActorPackage.Basic.ActorHome actor )
        {
        }

        /// <summary>
        /// This is automatically set from the <see cref="SqlTable.TableName"/>.
        /// </summary>
        public string SpecialName { get; protected set; }
    }
}
