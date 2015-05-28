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

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class AllDefaultValuesPackage : SqlPackage
    {
        [SqlProcedure( "sAllDefaultValues", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract string AllDefaultValues( SqlStandardCallContext ctx );

    }
}
