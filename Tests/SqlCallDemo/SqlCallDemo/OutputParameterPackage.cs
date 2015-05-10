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

    [SqlPackage( Schema = "CK", Database = typeof( SqlDefaultDatabase ), ResourceType = typeof( OutputParameterPackage ), ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class OutputParameterPackage : SqlPackage
    {
        /// <summary>
        /// A simple return type on an input/output parameter. The sql default value applies.
        /// <para>
        /// For stored procedure, it is enough to set the SqlCommandParameter to null (not to DBNull.Value) to trigger the use of
        /// the default value of the parameter. 
        /// But this does not work for table valued functions (see http://stackoverflow.com/questions/2970516/how-do-you-specify-default-as-a-sql-parameter-value-in-ado-net).
        /// For the table valued case, we HAVE TO manually inject the actual default value parsed from the header.
        /// This is why the sql expression of the default vallue is reinjected in the call. 
        /// </para>
        /// </summary>
        [SqlProcedure( "sOutputInputParameterWithDefault", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract string OutputInputParameterWithDefault( SqlStandardCallContext ctx );

        /// <summary>
        /// Injecting the default value of the (also) returned parameter.
        /// </summary>
        [SqlProcedure( "sOutputInputParameterWithDefault", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract string OutputInputParameterWithDefault( SqlStandardCallContext ctx, string textResult );

        /// <summary>
        /// A simple return type on a pure output parameter, a warning is emitted:
        /// if a pure output parameter has a default value then it should be marked /*input*/output since the input value seems to matter.
        /// </summary>
        [SqlProcedure( "sOutputParameterWithDefault", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract string OutputParameterWithDefault( SqlStandardCallContext ctx );

        /// <summary>
        /// A simple return type on a pure output parameter but with a value for the default (a warning is still emitted about the missing /*input*/ marker).
        /// </summary>
        [SqlProcedure( "sOutputParameterWithDefault", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        public abstract string OutputParameterWithDefault( SqlStandardCallContext ctx, string textResult );

        ///// <summary>
        ///// A simple return type on a pure output parameter but with a value for the default (a warning is still emitted about the missing /*input*/ marker).
        ///// </summary>
        //[SqlProcedure( "sOutputParameterWithDefault", ExecuteCall = ExecutionType.ExecuteNonQuery )]
        //public abstract Task<string> OutputParameterWithDefaultAsync( SqlStandardCallContext ctx, string textResult );

    }
}
