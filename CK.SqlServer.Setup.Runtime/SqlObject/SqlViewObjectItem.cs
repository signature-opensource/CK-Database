using CK.Setup;
using CK.SqlServer.Parser;
using System.Diagnostics;
using System;
using CK.Core;

namespace CK.SqlServer.Setup
{
    public class SqlViewObjectItem : SqlObjectItem
    {
        internal SqlViewObjectItem( SqlContextLocName name, ISqlServerView view )
            : base( name, "View", view )
        {
        }

    }
}
