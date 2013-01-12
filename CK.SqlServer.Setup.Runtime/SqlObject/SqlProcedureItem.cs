using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;

namespace CK.SqlServer.Setup
{
    public class SqlProcedureItem : SqlObjectItem
    {
        internal SqlProcedureItem( SqlObjectProtoItem p, MethodInfo m = null )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeProcedure );
            MethodInfo = m;
        }

        public MethodInfo MethodInfo { get; set; }

    }
}
