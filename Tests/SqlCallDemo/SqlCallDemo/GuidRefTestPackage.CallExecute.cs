using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo
{
    public abstract partial class GuidRefTestPackage
    {
        /// <summary>
        /// Calling the procedure: as long as the <see cref="ISqlCallContext"/> object exposes a public ExecuteNonQuery( string connectionString, SqlCommand cmd ) method,
        /// and the attribute specifies ExecuteCall = ExecutionType.ExecuteNonQuery, the call is executed.
        /// Here, we use the standard <see cref="SqlStandardCallContext"/>.
        /// </summary>

        // TODO !
        //[SqlProcedure( "sGuidRefTest", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        //public abstract void GuidRefTest( SqlStandardCallContext ctx, bool replaceInAndOut, Guid inOnly, ref Guid inAndOut, out string textResult );

    }
}
