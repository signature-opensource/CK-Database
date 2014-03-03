using System.Diagnostics;

namespace CK.SqlServer.Setup
{
    public class SqlViewObjectItem : SqlObjectItem
    {
        internal SqlViewObjectItem( SqlObjectProtoItem p )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeView );
        }
    }
}
