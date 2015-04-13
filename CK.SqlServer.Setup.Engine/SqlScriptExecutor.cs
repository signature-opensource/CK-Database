#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Engine\SqlScriptExecutor.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Diagnostics;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    class SqlScriptExecutor : MultiScriptExecutorBase
    {
        ISqlManager _manager;
        ISetupSessionMemory _memory;

        public SqlScriptExecutor( ISqlManager m, ISetupSessionMemory memory )
        {
            Debug.Assert( m != null );
            _manager = m;
            _memory = memory;
        }

        protected override MultiScriptBase CreateMultiScript( IActivityMonitor monitor, GenericItemSetupDriver driver, ISetupScript script )
        {
            return new SqlMultiScript( monitor, script, _manager, _memory );
        }

    }
}
