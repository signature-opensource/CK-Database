using System.Diagnostics;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    class SqlScriptExecutor : MultiScriptExecutorBase
    {
        SqlManager _manager;
        ISetupSessionMemory _memory;

        public SqlScriptExecutor( SqlManager m, ISetupSessionMemory memory )
        {
            Debug.Assert( m != null );
            _manager = m;
            _memory = memory;
        }

        protected override MultiScriptBase CreateMultiScript( IActivityMonitor monitor, SetupDriver driver, ISetupScript script )
        {
            return new SqlMultiScript( monitor, script, _manager, _memory );
        }

    }
}
