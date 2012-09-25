using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace CK.Setup.SqlServer
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

        protected override MultiScriptBase CreateMultiScript( IActivityLogger logger, SetupDriver driver, ISetupScript script )
        {
            return new SqlMultiScript( logger, script, _manager, _memory );
        }

    }
}
