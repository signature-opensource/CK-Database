using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup.Database;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{
    public class SqlUserTypeItem : SqlObjectItem
    {
        internal SqlUserTypeItem( SqlObjectProtoItem p )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeUserType );
        }
    }
}
