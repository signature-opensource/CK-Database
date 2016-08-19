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

    /// <summary>
    /// Demo package that exposes the CmdDemo command with its 
    /// two returned POCO (mutable and immutable).
    /// This is for demo only: obviously only one of them is enough.
    /// </summary>
    [SqlPackage( Schema = "Command", ResourcePath = "Res" ), Versions( "1.0.0" )]
    public abstract partial class CmdDemoPackage : SqlPackage
    {
        /// <summary>
        /// Executes the CmdDemo command and returns a mutable result object.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>A mutable result object.</returns>
        [SqlProcedure( "sCommandRun" )]
        public abstract Task<CmdDemo.ResultPOCO> RunCommandAsync( ISqlCallContext ctx, [ParameterSource]CmdDemo cmd );

        /// <summary>
        /// Executes the CmdDemo command and returns an immutable result object.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>An immutable result object.</returns>
        [SqlProcedure( "sCommandRun" )]
        public abstract Task<CmdDemo.ResultReadOnly> RunCommandROAsync( ISqlCallContext ctx, [ParameterSource]CmdDemo cmd );

    }
}
