using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using CK.Core;
using CK.Reflection;
using CK.SqlServer.Parser;
using System.Text;

namespace CK.SqlServer.Setup
{
    public class SqlFunctionTableItem : SqlCallableItem<ISqlServerFunctionTable>
    {
        internal SqlFunctionTableItem( SqlObjectProtoItem p, ISqlServerFunctionTable tableFunction )
            : base( p, tableFunction )
        {
        }
    }
}
