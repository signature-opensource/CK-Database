using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup.Database;

namespace CK.Setup.SqlServer
{
    public class SqlViewItem : SqlObjectItem
    {
        internal SqlViewItem( ReadInfo readInfo )
            : base( SqlObjectItem.TypeView, readInfo )
        {
        }
    }
}
