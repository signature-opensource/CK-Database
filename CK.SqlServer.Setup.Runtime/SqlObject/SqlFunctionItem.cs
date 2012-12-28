using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.SqlServer.Setup
{
    public class SqlFunctionItem : SqlObjectItem
    {
        internal SqlFunctionItem( SqlObjectProtoItem p )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeFunction );
        }
    }
}
