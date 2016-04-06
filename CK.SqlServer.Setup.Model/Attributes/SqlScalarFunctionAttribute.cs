using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    public class SqlScalarFunctionAttribute : SqlCallableAttributeBase
    {
        public SqlScalarFunctionAttribute( string functionName )
            : base( functionName, "Function" )
        {
            ExecuteCall = ExecutionType.ExecuteNonQuery;
        }
    }
}
