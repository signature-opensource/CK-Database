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

        public SqlScriptTypeHandler( ISqlManagerProvider provider )
        {
            if( provider == null ) throw new ArgumentNullException( "provider" );
            _managerProvider = provider;
        }

        protected override IScriptExecutor CreateExecutor( IActivityLogger logger, SetupDriver driver )
        {
            if( driver == null ) throw new ArgumentNullException( "driver" );
            SqlManager m = FindManagerFromLocation( logger, _managerProvider, driver.FullName );
            return m != null ? new SqlScriptExecutor( m, driver.Engine.Memory ) : null;
        }

        protected override void ReleaseExecutor( IActivityLogger logger, IScriptExecutor executor )
        {           
        }

        public static SqlManager FindManagerFromLocation( IActivityLogger logger, ISqlManagerProvider provider, string fullName )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( provider == null ) throw new ArgumentNullException( "provider" );
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            SqlManager m = null;
            string context, location, name;
            if( !DefaultContextLocNaming.TryParse( fullName, out context, out location, out name ) || String.IsNullOrEmpty( location ) )
            {
                logger.Error( "Unable to extract a location from FullName '{0}' in order to find a Sql connection.", fullName );
            }
            else if( (m = provider.FindManagerByName( location )) == null )
            {
                logger.Error( "Location '{0}' from FullName '{1}' can not be mapped to an existing Sql Connection.", location, fullName );
            }
            return m;
        }

    }
}
