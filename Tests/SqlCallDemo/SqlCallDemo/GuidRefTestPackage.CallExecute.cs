using System;
using CK.Core;
using CK.SqlServer;

namespace SqlCallDemo
{
    public abstract partial class GuidRefTestPackage
    {
        /// <summary>
        /// Calling the procedure: as long as a parameter implements <see cref="ISqlCommandExecutor"/>,
        /// and the attribute specifies ExecuteCall = ExecutionType.ExecuteNonQuery, the call is executed.
        /// Here, we use the standard <see cref="SqlStandardCallContext"/>.
        /// When mutiple ISqlCallContext parameters occur, the first one that can handle the call (ie. the firs Executor) 
        /// will be used.
        /// </summary>
        [SqlProcedure( "sGuidRefTest" )]
        public abstract void GuidRefTest( ISqlCallContext ctx, bool replaceInAndOut, Guid inOnly, ref Guid inAndOut, out string textResult );

        /// <summary>
        /// When a returned type exists, its corresponds to the last output or input/output parameter with a compatible type.
        /// </summary>
        [SqlProcedure( "sGuidRefTest" )]
        public abstract string GuidRefTestReturn( ISqlCallContext ctx, bool replaceInAndOut, Guid inOnly, ref Guid inAndOut );

        /// <summary>
        /// Any output with a compatible type can be used (here the inAndOut unique identifier is returned). The returned value is always the 
        /// one that corresponds to the last compatible type.
        /// </summary>
        [SqlProcedure( "sGuidRefTest" )]
        public abstract Guid GuidRefTestReturnInOut( ISqlCallContext ctx, bool replaceInAndOut, Guid inOnly, Guid inAndOut, out string textResult );

    }
}
