using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;
using CK.Setup.Database;

namespace CK.Setup.SqlServer
{
    public class SqlScriptTypeHandler : ScriptTypeHandler
    {
        ISqlManagerProvider _managerProvider;
        IScriptExecutor _executor;

        public SqlScriptTypeHandler( ISqlManagerProvider provider )
        {
            if( provider == null ) throw new ArgumentNullException( "provider" );
            _managerProvider = provider;
        }

        protected override IScriptExecutor CreateExecutor( IActivityLogger logger, SetupDriver driver )
        {
            SqlManager m = _managerProvider.FindManagerByName( SqlDatabase.DefaultDatabaseName );
            return _executor ?? (_executor = new SqlScriptExecutor( m, driver.Engine.Memory ));
        }

        protected override void ReleaseExecutor( IActivityLogger logger, IScriptExecutor executor )
        {           
        }

    }
}
