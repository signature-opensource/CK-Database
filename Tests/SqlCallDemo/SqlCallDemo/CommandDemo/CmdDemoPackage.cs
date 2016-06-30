using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo.CommandDemo
{

    [SqlPackage( Schema = "Command", ResourcePath = "Res" ), Versions( "1.0.0" )]
    public abstract partial class CmdDemoPackage : SqlPackage
    {
        [SqlProcedure( "sCommandRun" )]
        public abstract Task<CmdDemo.ResultPOCO> RunCommandAsync( ISqlCallContext ctx, [ParameterSource]CmdDemo cmd );

        [SqlProcedure( "sCommandRun" )]
        public abstract Task<CmdDemo.ResultReadOnly> RunCommandROAsync( ISqlCallContext ctx, [ParameterSource]CmdDemo cmd );

    }
}
