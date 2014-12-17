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
    public class SqlDatabaseConnectionSetupDriver : SetupDriver
    {
        readonly ISqlManagerProvider _sqlProvider;
        ISqlManager _connection;

        public SqlDatabaseConnectionSetupDriver( BuildInfo info, ISqlManagerProvider sqlProvider )
            : base( info )
        {
            if( sqlProvider == null ) throw new ArgumentNullException( "sqlProvider" );
            _sqlProvider = sqlProvider;
        }

        public new SqlDatabaseConnectionItem Item { get { return (SqlDatabaseConnectionItem)base.Item; } }

        protected override bool Init()
        {
            _connection = FindManager( _sqlProvider, Engine.Monitor, Item.SqlDatabase );
            if( _connection == null ) return false;
            foreach( var name in Item.SqlDatabase.Schemas )
            {
                _connection.ExecuteOneScript( String.Format( "if not exists(select 1 from sys.schemas where name = '{0}') begin exec( 'create schema {0}' ); end", name ), Engine.Monitor );
            }
            return base.Init();
        }

        static ISqlManager FindManager( ISqlManagerProvider sql, IActivityMonitor monitor, SqlDatabase db )
        {
            ISqlManager c = null;
            if( !String.IsNullOrWhiteSpace( db.ConnectionString ) )
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
