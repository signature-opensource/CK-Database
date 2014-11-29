using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlSetupAspect : ISqlManagerProvider, IDisposable, IStObjBuilder
    {
        readonly SqlSetupAspectConfiguration _config;
        readonly SetupCenter _center;
        readonly ISqlManager _defaultDatabase;
        readonly SqlManagerProvider _databases;
        readonly SqlFileDiscoverer _sqlFileDiscoverer;
        readonly DependentProtoItemCollector _sqlFiles;

        class ConfiguratorHook : SetupableConfigurator
        {
            readonly SqlSetupAspect _center;

            public ConfiguratorHook( SqlSetupAspect sqlCenter )
                : base( sqlCenter._center.SetupableConfigurator )
            {
                _center = sqlCenter;
            }

            public override void ResolveParameterValue( IActivityMonitor monitor, IStObjFinalParameter parameter )
            {
                base.ResolveParameterValue( monitor, parameter );
                if( parameter.Name == "connectionString" )
                {
                    SqlDatabase db = parameter.Owner.InitialObject as SqlDatabase;
                    if( db != null )
                    {
                        parameter.SetParameterValue( _center._config.FindConnectionStringByName( db.Name ) );
                    }
                }
            } 

            public override SetupDriver CreateDriver( Type driverType, SetupDriver.BuildInfo info )
            {
                SetupDriver d = base.CreateDriver( driverType, info );
                if( d == null )
                {
                    if( driverType == typeof( SqlObjectSetupDriver ) ) d = new SqlObjectSetupDriver( info, _center );
                    else if( driverType == typeof( SetupDriver ) ) d = new SetupDriver( info );
                    else if( driverType == typeof( SqlDatabaseSetupDriver ) ) d = new SqlDatabaseSetupDriver( info );
                    else if( driverType == typeof( SqlDatabaseConnectionSetupDriver ) ) d = new SqlDatabaseConnectionSetupDriver( info, _center );
                    else if( driverType == typeof( SqlPackageBaseSetupDriver ) ) d = new SqlPackageBaseSetupDriver( info );
                    else if( driverType == typeof( SqlTableSetupDriver ) ) d = new SqlTableSetupDriver( info );
                    else if( driverType == typeof( SqlViewSetupDriver ) ) d = new SqlViewSetupDriver( info, _center );
                }
                return d;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="SqlSetupAspect"/> with a <see cref="DefaultSqlDatabase"/> that uses the configuration (<see cref="SqlSetupAspectConfiguration.DefaultDatabaseConnectionString"/>)
        /// for its connection string.
        /// This constructor is the one used when calling <see cref="StObjContextRoot.Build"/> method with a <see cref="SqlSetupAspectConfiguration"/> configuration object.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="config">Configuration object.</param>
        public SqlSetupAspect( IActivityMonitor monitor, SqlSetupAspectConfiguration config, IStObjRuntimeBuilder runtimeBuilder = null )
            : this( monitor, config, runtimeBuilder, null )
        {
        }

        bool IStObjBuilder.Run()
        {
            return _center.Run();
        }

        public SqlSetupAspect( IActivityMonitor monitor, SqlSetupAspectConfiguration config, IStObjRuntimeBuilder runtimeBuilder, ISqlManager defaultDatabase )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( config == null ) throw new ArgumentNullException( "config" );
            _config = config;

            _databases = new SqlManagerProvider( monitor, m => m.IgnoreMissingDependencyIsError = _config.IgnoreMissingDependencyIsError );
            if( defaultDatabase == null )
            {
                _databases.Add( SqlDatabase.DefaultDatabaseName, _config.DefaultDatabaseConnectionString, autoCreate:true );
                _defaultDatabase = _databases.FindManagerByName( SqlDatabase.DefaultDatabaseName );
            }
            else
            {
                if( !defaultDatabase.IsOpen() ) throw new ArgumentException( "Database manager must be opened.", "defaultDatabase" );
                defaultDatabase.IgnoreMissingDependencyIsError = config.IgnoreMissingDependencyIsError;
                _databases.AddConfiguredDefaultDatabase( defaultDatabase );
                _defaultDatabase = defaultDatabase;
            }
            foreach( var db in _config.Databases )
            {
                _databases.Add( db.DatabaseName, db.ConnectionString, db.AutoCreate );
            }
            
            var versionRepo = new SqlVersionedItemRepository( _defaultDatabase );
            var memory = new SqlSetupSessionMemoryProvider( _defaultDatabase );
            _center = new SetupCenter( monitor, _config.SetupConfiguration, versionRepo, memory, runtimeBuilder );
            _sqlFiles = new DependentProtoItemCollector();
            _sqlFileDiscoverer = new SqlFileDiscoverer( new SqlObjectParser(), monitor );
            _center.SetupableConfigurator = new ConfiguratorHook( this );

            var sqlHandler = new SqlScriptTypeHandler( this );
            // Registers source "res-sql" first: resource scripts have low priority.
            sqlHandler.RegisterSource( "res-sql" );
            // Then registers "file-sql".
            sqlHandler.RegisterSource( SqlFileDiscoverer.DefaultSourceName );

            _center.ScriptTypeManager.Register( sqlHandler );

            _center.RegisterSetupEvent += OnRegisterSetup;
        }

        /// <summary>
        /// Gets the setup center.
        /// </summary>
        public SetupCenter Center
        {
            get { return _center; }
        }

        /// <summary>
        /// Gets or sets a <see cref="SetupableConfigurator"/> that will be used.
        /// This can be changed at any moment during setup: the current configurator will always be used.
        /// When setting it, care should be taken to not break the chain by setting the current configurator as the <see cref="SetupableConfigurator.Previous"/>.
        /// </summary>
        public SetupableConfigurator SetupableConfigurator
        {
            get { return _center.SetupableConfigurator; }
            set { _center.SetupableConfigurator = value; }
        }

        /// <summary>
        /// Discovers file packages (*.ck xml files) in given directory and sub directories.
        /// </summary>
        /// <param name="directoryPath">Directory from where *.ck files must be registered.</param>
        /// <returns>True if no error occurred. Errors are logged.</returns>
        public bool DiscoverFilePackages( string directoryPath )
        {
            return _sqlFileDiscoverer.DiscoverPackages( String.Empty, SqlDefaultDatabase.DefaultDatabaseName, directoryPath );
        }

        /// <summary>
        /// Discovers sql files in given directory and sub directories.
        /// </summary>
        /// <param name="directoryPath">Directory from where *.sql files must be registered.</param>
        /// <returns>True if no error occurred. Errors are logged.</returns>
        public bool DiscoverSqlFiles( string directoryPath )
        {
            return _sqlFileDiscoverer.DiscoverSqlFiles( String.Empty, SqlDefaultDatabase.DefaultDatabaseName, directoryPath, _sqlFiles, _center.Scripts );
        }

        /// <summary>
        /// Gets or sets a function that will be called with the list of StObjs once all of them are 
        /// registered in the <see cref="DependencySorter"/> used by the <see cref="StObjCollector"/>.
        /// </summary>
        public Action<IEnumerable<IDependentItem>> StObjDependencySorterHookInput
        {
            get { return _center.StObjDependencySorterHookInput; }
            set { _center.StObjDependencySorterHookInput = value; }
        }

        /// <summary>
        /// Gets or sets a function that will be called when StObjs have been successfully sorted by 
        /// the <see cref="DependencySorter"/> used by the <see cref="StObjCollector"/>.
        /// </summary>
        public Action<IEnumerable<ISortedItem>> StObjDependencySorterHookOutput
        {
            get { return _center.StObjDependencySorterHookOutput; }
            set { _center.StObjDependencySorterHookOutput = value; } 
        }

        /// <summary>
        /// Gets or sets a function that will be called with the list of items once all of them are registered.
        /// </summary>
        public Action<IEnumerable<IDependentItem>> SetupDependencySorterHookInput 
        {
            get { return _center.DependencySorterHookInput; }
            set { _center.DependencySorterHookInput = value; } 
        }

        /// <summary>
        /// Gets or sets a function that will be called when items have been successfully sorted.
        /// </summary>
        public Action<IEnumerable<ISortedItem>> SetupDependencySorterHookOutput
        {
            get { return _center.DependencySorterHookOutput; }
            set { _center.DependencySorterHookOutput = value; }
        }

        /// <summary>
        /// Gets the default database as a <see cref="SqlManager"/> object.
        /// </summary>
        public ISqlManager DefaultSqlDatabase
        {
            get { return _defaultDatabase; }
        }

        /// <summary>
        /// Gets the available databases (including the <see cref="DefaultSqlDatabase"/>).
        /// It is initialized with <see cref="SqlSetupAspectConfiguration.Databases"/> content but can be changed.
        /// </summary>
        public SqlManagerProvider SqlDatabases
        {
            get { return _databases; }
        }

        void OnRegisterSetup( object sender, RegisterSetupEventArgs e )
        {
            var monitor = _center.Logger;

            bool hasError = false;
            using( monitor.CatchCounter( a => hasError = true ) )
            {
                if( _config.FilePackageDirectories.Count > 0 )
                {
                    using( monitor.OpenInfo().Send( "Discovering *.ck packages files from {0} directories.", _config.FilePackageDirectories.Count ) )
                    {
                        foreach( string d in _config.FilePackageDirectories )
                        {
                            DiscoverFilePackages( d );
                        }
                        e.Register( _sqlFileDiscoverer.DiscoveredPackages );
                    }
                }
                if( _config.SqlFileDirectories.Count > 0 )
                {
                    using( monitor.OpenInfo().Send( "Discovering Sql files from {0} directories.", _config.SqlFileDirectories.Count ) )
                    {
                        foreach( string d in _config.SqlFileDirectories )
                        {
                            DiscoverSqlFiles( d );
                        }
                    }
                }
                if( !hasError )
                {
                    using( monitor.OpenInfo().Send( "Creating Sql Objects from {0} sql files.", _sqlFiles.Count ) )
                    {
                        List<IDependentItem> items = new List<IDependentItem>();
                        foreach( var proto in _sqlFiles.OfType<SqlObjectProtoItem>() )
                        {
                            var item = proto.CreateItem( monitor );
                            if( item == null ) hasError = true;
                            else items.Add( item );
                        }
                        if( hasError ) monitor.Info().Send( "At least one Sql Object creation failed." );
                        else e.Register( items );
                    }
                }
            }
            if( hasError )
            {
                e.CancelSetup( "Error while registering files." );
            }
        }

        ISqlManager ISqlManagerProvider.FindManagerByName( string dbName )
        {
            if( dbName == null ) throw new ArgumentNullException( "dbName" );
            if( dbName == SqlDatabase.DefaultDatabaseName ) return _defaultDatabase;
            ISqlManager m = ObtainManager( dbName );
            if( m == null ) _center.Logger.Warn().Send( "Database named '{0}' is not mapped.", dbName );
            return m;
        }

        ISqlManager ISqlManagerProvider.FindManagerByConnectionString( string conString )
        {
            if( conString == null ) throw new ArgumentNullException( "conString" );
            if( conString == _defaultDatabase.Connection.ConnectionString ) return _defaultDatabase;
            ISqlManager m = ObtainManagerByConnectionString( conString );
            if( m == null ) _center.Logger.Warn().Send( "Database connection to '{0}' is not mapped.", conString );
            return m;
        }

        protected virtual ISqlManager ObtainManager( string dbName )
        {
            return _databases.FindManagerByName( dbName );
        }

        protected virtual ISqlManager ObtainManagerByConnectionString( string conString )
        {
            return _databases.FindManagerByConnectionString( conString );
        }

        /// <summary>
        /// Releases all database managers.
        /// Can safely be called multiple times.
        /// </summary>
        public virtual void Dispose()
        {
            // Can safely be called multiple times.
            _databases.Dispose();
        }

    }
}
