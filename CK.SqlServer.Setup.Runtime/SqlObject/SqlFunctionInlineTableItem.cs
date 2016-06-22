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
    public class SqlFunctionInlineTableItem : SqlCallableItem<ISqlServerFunctionInlineTable>
    {
        internal SqlFunctionInlineTableItem( SqlContextLocName name, ISqlServerFunctionInlineTable inlineFunction )
            : base( name, "Function", inlineFunction )
        {
        }
    }
}
