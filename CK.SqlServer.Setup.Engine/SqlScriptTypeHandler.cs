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

        protected override IScriptExecutor CreateExecutor( IActivityLogger logger, SetupDriver driver )
        {
            if( driver == null ) throw new ArgumentNullException( "driver" );
            SqlManager m = SqlObjectSetupDriver.FindManagerFromLocation( logger, _managerProvider, driver.FullName );
            return m != null ? new SqlScriptExecutor( m, driver.Engine.Memory ) : null;
        }

        protected override void ReleaseExecutor( IActivityLogger logger, IScriptExecutor executor )
        {           
        }

    }
}
