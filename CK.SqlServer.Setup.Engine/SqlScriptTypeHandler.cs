#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Engine\SqlScriptTypeHandler.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlScriptTypeHandler : ScriptTypeHandler
    {
        ISqlManagerProvider _managerProvider;

        public SqlScriptTypeHandler( ISqlManagerProvider provider )
        {
            if( provider == null ) throw new ArgumentNullException( "provider" );
            _managerProvider = provider;
        }

        protected override IScriptExecutor CreateExecutor( IActivityMonitor monitor, SetupItemDriver driver )
        {
            if( driver == null ) throw new ArgumentNullException( "driver" );
            ISqlManager m = SqlObjectSetupDriver.FindManagerFromLocation( monitor, _managerProvider, driver.FullName );
            return m != null ? new SqlScriptExecutor( m, driver.Engine.Memory ) : null;
        }

        protected override void ReleaseExecutor( IActivityMonitor monitor, IScriptExecutor executor )
        {           
        }

    }
}
