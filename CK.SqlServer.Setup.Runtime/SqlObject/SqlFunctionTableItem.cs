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
    /// A sql table function item is a specialized <see cref="SqlCallableItem{T}"/> where T, the SqlObject,
    /// is a <see cref="ISqlServerFunctionTable"/>.
    /// </summary>
    public class SqlFunctionTableItem : SqlCallableItem<ISqlServerFunctionTable>
    {
        /// <summary>
        /// Initializes a table function item with its name and code.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        /// <param name="view">Code of the function.</param>
        public SqlFunctionTableItem( SqlContextLocName name, ISqlServerFunctionTable tableFunction )
            : base( name, "Function", tableFunction )
        {
        }
    }
}
