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
            ISqlManagerBase m = FindManagerFromLocation( monitor, _managerProvider, driver.FullName );
            return m != null ? new SqlScriptExecutor( m, driver.Engine.Memory ) : null;
        }

        protected override void ReleaseExecutor( IActivityMonitor monitor, IScriptExecutor executor )
        {           
        }


        static ISqlManagerBase FindManagerFromLocation( IActivityMonitor monitor, ISqlManagerProvider provider, string fullName )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( provider == null ) throw new ArgumentNullException( "provider" );
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            ISqlManagerBase m = null;
            string context, location, name, targetName;
            if( !DefaultContextLocNaming.TryParse( fullName, out context, out location, out name, out targetName ) )
            {
                monitor.Error().Send( "Unable to extract a location from FullName '{0}' in order to find a Sql connection.", fullName );
            }
            else
            {
                if( (m = provider.FindManagerByName( location )) == null )
                {
                    monitor.Error().Send( "Location '{0}' from FullName '{1}' can not be mapped to an existing Sql Connection.", location, fullName );
                }
            }
            return m;
        }

    }
}
