using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup.Database;
using System.Diagnostics;
using System.Reflection;

namespace CK.Setup.SqlServer
{
    public class SqlProcedureItem : SqlObjectItem
    {
        readonly MethodInfo _m;

        internal SqlProcedureItem( SqlObjectProtoItem p, MethodInfo m = null )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeProcedure );
            _m = m;
        }


    }
}
