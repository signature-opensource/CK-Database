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
    /// <summary>
    /// A sql inline table function item is a specialized <see cref="SqlCallableItem{T}"/> where T, the SqlObject,
    /// is a <see cref="ISqlServerFunctionInlineTable"/>.
    /// </summary>
    public class SqlFunctionInlineTableItem : SqlCallableItem<ISqlServerFunctionInlineTable>
    {
        /// <summary>
        /// Initializes an inline table function item with its name and code.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        /// <param name="view">Code of the function.</param>
        public SqlFunctionInlineTableItem( SqlContextLocName name, ISqlServerFunctionInlineTable inlineFunction )
            : base( name, "Function", inlineFunction )
        {
        }
    }
}
