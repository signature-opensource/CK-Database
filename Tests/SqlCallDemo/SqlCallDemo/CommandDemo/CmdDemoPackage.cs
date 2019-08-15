using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;

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


        /// <summary>
        /// Real case example.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting user.</param>
        /// <param name="data">The payload.</param>
        /// <returns></returns>
        [SqlProcedure( "sProtoUserCreate" )]
        public abstract int CreateProtoUser( ISqlCallContext ctx, int actorId, [ParameterSource]ProtoUserData data );
    }
}
