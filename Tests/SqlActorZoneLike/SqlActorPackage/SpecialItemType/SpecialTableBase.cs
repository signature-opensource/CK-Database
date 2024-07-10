using CK.Core;

namespace SqlActorPackage.SpecialItemType
{
    [CKTypeDefiner]
    [Setup( ItemTypeName = "SqlActorPackage.Engine.SpecialTableBaseItem, SqlActorPackage.Engine" )]
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
