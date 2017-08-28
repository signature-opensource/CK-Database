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

        public SqlDatabaseConnectionItemDriver( BuildInfo info, ISqlManagerProvider sqlProvider )
            : base( info )
        {
            _sqlProvider = sqlProvider;
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
                _connection.ExecuteOneScript( @"
-- Ensure that snapshot_isolation and read_committed_snapshot are on for this db.
declare @dbName sysname = DB_NAME();
declare @dbNameQ sysname = QUOTENAME(@dbName);
declare @isSingleUser bit = 0;
declare @isRCSEnabled int;
select @isRCSEnabled = is_read_committed_snapshot_on from sys.databases where name = @dbName;
if @isRCSEnabled = 0
begin
    exec( 'alter database '+@dbNameQ+' set single_user with rollback immediate;' );
    set @isSingleUser = 1;
    exec( 'alter database '+@dbNameQ+' set read_committed_snapshot on;' );
end;
 
declare @isSIEnabled int;
select @isSIEnabled = snapshot_isolation_state from sys.databases where name = @dbName;
if @isSIEnabled = 0
begin
    if @isSingleUser = 0 exec ('alter database ' + @dbNameQ + ' set single_user with rollback immediate;');
    exec( 'alter database '+@dbNameQ+' set allow_snapshot_isolation on;' );
end;
 
if @isSingleUser = 1 exec( 'alter database '+@dbNameQ+' set multi_user;' );
", Engine.Monitor );
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
                monitor.Error( $"Database '{db.Name}' not available." );
            }
            else if( !db.IsDefaultDatabase && db.InstallCore )
            {
                c.EnsureCKCoreIsInstalled( monitor );
            }
            return c;
        }
    }

}
