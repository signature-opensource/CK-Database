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
        readonly SqlExprStStoredProc _storedProc;

        internal SqlProcedureItem( SqlObjectProtoItem p, SqlExprStStoredProc storedProc = null, MethodInfo m = null )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeProcedure );
            MethodInfo = m;
            _storedProc = storedProc;
        }

        public MethodInfo MethodInfo { get; set; }

        /// <summary>
        /// Gets the original parsed stored procedure. 
        /// Can be null if an error occured during parsing.
        /// </summary>
        public SqlExprStStoredProc OriginalStatement { get { return _storedProc; } }

    }
}
