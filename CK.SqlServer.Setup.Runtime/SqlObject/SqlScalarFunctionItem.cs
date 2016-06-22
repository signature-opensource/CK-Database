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
    public class SqlFunctionScalarItem : SqlCallableItem<ISqlServerFunctionScalar>
    {
        internal SqlFunctionScalarItem( SqlContextLocName name, ISqlServerFunctionScalar scalarFunction )
            : base( name, "Function", scalarFunction )
        {
        }
    }
}
