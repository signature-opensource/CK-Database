using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Driver for <see cref="SqlDatabaseConnectionItem"/> item. 
    /// </summary>
    public class SqlDatabaseConnectionItemDriver : SetupItemDriver
    {
        readonly ISqlManagerProvider _sqlProvider;
        ISqlManagerBase _connection;

        /// <summary>
        /// Initializes a new <see cref="SqlDatabaseConnectionItem"/>.
        /// </summary>
        /// <param name="info">Driver build information (required by base SetupItemDriver).</param>
        /// <param name="sqlProvider">The sql manager provider.</param>
        public SqlDatabaseConnectionItemDriver( BuildInfo info, ISqlManagerProvider sqlProvider )
            : base( info )
        {
            _sqlProvider = sqlProvider;
        }

        /// <summary>
        /// Masked Item to formally be associated to a <see cref="SqlDatabaseConnectionItem"/> item.
        /// </summary>
        public new SqlDatabaseConnectionItem Item => (SqlDatabaseConnectionItem)base.Item;

        /// <summary>
        /// Gets the Sql manager. This is initialized by <see cref="ExecutePreInit"/>.
        /// </summary>
        public ISqlManagerBase SqlManager => _connection;

        /// <summary>
        /// Initializes the <see cref="SqlManager"/> based on the <see cref="Item"/>'s <see cref="SqlDatabaseConnectionItem.SqlDatabase"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false if the database can not be found in the <see cref="ISqlManagerProvider"/>.</returns>
        protected override bool ExecutePreInit( IActivityMonitor monitor )
        {
            _connection = FindManager( _sqlProvider, monitor, Item.SqlDatabase );
            return _connection != null;
        }

        /// <summary>
        /// Initializes the database by ensuring that all <see cref="Item"/>'s registered <see cref="SqlDatabase.Schemas"/>
        /// exists and that snapshot_isolation and read_committed_snapshot are on for this db. 
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="beforeHandlers">Initialization is done after the handlers (when true, this method does nothing).</param>
        /// <returns>True on success, false if an error occurred.</returns>
        protected override bool Init( IActivityMonitor monitor, bool beforeHandlers )
        {
            if( !beforeHandlers )
            {
                foreach( var name in Item.SqlDatabase.Schemas )
                {
                    string sqlName = name.Replace( "]", "]]" );
                    _connection.ExecuteOneScript( $"if not exists(select 1 from sys.schemas where name = '{name}') begin exec( 'create schema [{sqlName}]' ); end", monitor );
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
", monitor );
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
