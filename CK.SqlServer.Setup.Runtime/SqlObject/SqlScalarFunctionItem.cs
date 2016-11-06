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
    /// A sql scalar function item is a specialized <see cref="SqlCallableItem{T}"/> where T, the SqlObject,
    /// is a <see cref="ISqlServerFunctionScalar"/>.
    /// </summary>
    public class SqlFunctionScalarItem : SqlCallableItem<ISqlServerFunctionScalar>
    {
        /// <summary>
        /// Initializes a scalar function item with its name and code.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        /// <param name="view">Code of the function.</param>
        public SqlFunctionScalarItem( SqlContextLocName name, ISqlServerFunctionScalar scalarFunction )
            : base( name, "Function", scalarFunction )
        {
        }
    }
}
