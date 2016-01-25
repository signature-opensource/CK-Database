using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    public class SqlProcedureNonQueryAttribute : SqlProcedureAttribute
    {
        public SqlProcedureNonQueryAttribute( string procedureName )
            : base( procedureName )
        {
            ExecuteCall = ExecutionType.ExecuteNonQuery;
        }
    }
}
