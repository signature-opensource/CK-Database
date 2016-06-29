using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    public class SqlProcedureAttribute : SqlCallableAttributeBase
    {
        public SqlProcedureAttribute( string procedureName )
            : base( procedureName, "Procedure" )
        {
            ExecuteCall = ExecutionType.ExecuteNonQuery;
        }
    }
}
