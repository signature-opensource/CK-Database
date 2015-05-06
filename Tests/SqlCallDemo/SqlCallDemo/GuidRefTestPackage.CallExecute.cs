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

    public interface INonStandardSqlCallContext : ISqlCallContext
    {
        void ExecuteNonQuery( string connectionString, SqlCommand cmd );
    }

    public interface INonStandardSqlCallContextSpecialized : INonStandardSqlCallContext
    {
    }

    public abstract partial class GuidRefTestPackage
    {
        /// <summary>
        /// Calling the procedure: as long as the <see cref="ISqlCallContext"/> object exposes a public ExecuteNonQuery( string connectionString, SqlCommand cmd ) method,
        /// and the attribute specifies ExecuteCall = ExecutionType.ExecuteNonQuery, the call is executed.
        /// Here, we use the standard <see cref="SqlStandardCallContext"/>.
        /// When mutiple ISqlCallContext parameters occur, the first one that can handle the call will be used.
        /// </summary>
        [SqlProcedure( "sGuidRefTest", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract void GuidRefTest( SqlStandardCallContext ctx, bool replaceInAndOut, Guid inOnly, ref Guid inAndOut, out string textResult );

        /// <summary>
        /// When a returned type exists, its corresponds to the last output or input/output parameter with a compatible type.
        /// </summary>
        [SqlProcedure( "sGuidRefTest", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract string GuidRefTestReturn( SqlStandardCallContext ctx, bool replaceInAndOut, Guid inOnly, ref Guid inAndOut );

        /// <summary>
        /// Calling with an interface that exposes the ExecuteNonQuery is possible (even a specialized one).
        /// </summary>
        [SqlProcedure( "sGuidRefTest", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract string GuidRefTestReturnWithInterfaceContext( INonStandardSqlCallContextSpecialized ctx, bool replaceInAndOut, Guid inOnly, ref Guid inAndOut );


        /// <summary>
        /// Any output with a compatible type can be used (here the inAndOut unique identifier is returned). The returned value is always the 
        /// one that corresponds to the last compatible type.
        /// </summary>
        [SqlProcedure( "sGuidRefTest", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract Guid GuidRefTestReturnInOut( SqlStandardCallContext ctx, bool replaceInAndOut, Guid inOnly, Guid inAndOut, out string textResult );

        // TODO
        ///// <summary>
        ///// Any type that have a constructor with parameters that can be matched with output parameters can be returned: this works for Tuple.
        ///// </summary>
        //[SqlProcedure( "sGuidRefTest", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        //public abstract Tuple<string,Guid> GuidRefTestReturnTuple( SqlStandardCallContext ctx, bool replaceInAndOut, Guid inOnly, Guid inAndOut );

    }
}
