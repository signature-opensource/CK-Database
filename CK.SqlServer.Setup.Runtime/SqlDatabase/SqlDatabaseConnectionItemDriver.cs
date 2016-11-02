#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlDatabase\SqlDatabaseConnectionSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseConnectionItemDriver : SetupItemDriver
    {
        readonly ISqlManagerProvider _sqlProvider;
        ISqlManagerBase _connection;

        public SqlDatabaseConnectionItemDriver( BuildInfo info )
            : base( info )
        {
            _sqlProvider = info.Engine.GetSetupEngineAspect<ISqlSetupAspect>().SqlDatabases;
        }

        public new SqlDatabaseConnectionItem Item => (SqlDatabaseConnectionItem)base.Item;

        /// <summary>
        /// Gets the Sql manager. This is initialized by <see cref="ExecutePreInit"/>.
        /// </summary>
        public ISqlManagerBase SqlManager => _connection;

        protected override bool ExecutePreInit()
        {
            _connection = FindManager( _sqlProvider, Engine.Monitor, Item.SqlDatabase );
            return _connection != null;
        }

        protected override bool Init( bool beforeHandlers )
        {
            if( !beforeHandlers )
            {
                foreach( var name in Item.SqlDatabase.Schemas )
                {
                    string sqlName = name.Replace( "]", "]]" );
                    _connection.ExecuteOneScript( $"if not exists(select 1 from sys.schemas where name = '{name}') begin exec( 'create schema [{sqlName}]' ); end", Engine.Monitor );
                }
            }
            return true;
        }

        static ISqlManagerBase FindManager( ISqlManagerProvider sql, IActivityMonitor monitor, SqlDatabase db )
        {
            ISqlManagerBase c = null;
            if( !string.IsNullOrWhiteSpace( db.ConnectionString ) )
            {
                c = sql.FindManagerByConnectionString( db.ConnectionString );
            }
            if( c == null )
            {
                c = sql.FindManagerByName( db.Name );
            }
            if( c == null )
            {
                monitor.Error().Send( "Database '{0}' not available.", db.Name );
            }
            else if( !db.IsDefaultDatabase && db.InstallCore )
            {
                c.EnsureCKCoreIsInstalled( monitor );
            }
            return c;
        }
    }

}
