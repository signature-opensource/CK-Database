#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Engine\SqlSetupAspect.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlSetupAspect : ISetupEngineAspect, ISqlManagerProvider, IDisposable
    {
        readonly SqlSetupAspectConfiguration _config;
        readonly SetupEngine _engine;
        readonly SqlManagerProvider _databases;
        readonly SqlFileDiscoverer _sqlFileDiscoverer;
        readonly DependentProtoItemCollector _sqlFiles;
        ISqlManager _defaultDatabase;

        class ConfiguratorHook : SetupEngineConfigurator
        {
            readonly SqlSetupAspect _center;

            public ConfiguratorHook( SqlSetupAspect sqlAspect )
                : base( sqlAspect._engine.SetupableConfigurator )
            {
                _center = sqlAspect;
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
        }

        /// <summary>
        /// Initializes a new <see cref="SqlSetupAspect"/>.
        /// This constructor is called by the <see cref="SetupEngine"/> whenever a <see cref="SqlSetupAspectConfiguration"/> configuration object
        /// appears in <see cref="SetupEngineConfiguration.Aspects"/> list.
        /// </summary>
        /// <param name="engine">Current engine.</param>
        /// <param name="config">Configuration object.</param>
        public SqlSetupAspect( SetupEngine engine, SqlSetupAspectConfiguration config )
        {
            if( engine == null ) throw new ArgumentNullException( "engine" );
            if( config == null ) throw new ArgumentNullException( "config" );
            _config = config;
            _engine = engine;
            _databases = new SqlManagerProvider( _engine.Monitor, m => m.IgnoreMissingDependencyIsError = _config.IgnoreMissingDependencyIsError );
            _databases.Add( SqlDatabase.DefaultDatabaseName, _config.DefaultDatabaseConnectionString, autoCreate:true );
            foreach( var db in _config.Databases )
            {
                _databases.Add( db.DatabaseName, db.ConnectionString, db.AutoCreate );
            }
            _sqlFiles = new DependentProtoItemCollector();
            _sqlFileDiscoverer = new SqlFileDiscoverer( new SqlObjectParser(), _engine.Monitor );
        }

        bool ISetupEngineAspect.Configure()
        {
            _defaultDatabase = _databases.FindManagerByName( SqlDatabase.DefaultDatabaseName );
            _engine.StartConfiguration.VersionRepository = new SqlVersionedItemRepository( _defaultDatabase );
            _engine.StartConfiguration.SetupSessionMemoryProvider = new SqlSetupSessionMemoryProvider( _defaultDatabase );

            _engine.SetupableConfigurator = new ConfiguratorHook( this );
            var sqlHandler = new SqlScriptTypeHandler( this );
            // Registers source "res-sql" first: resource scripts have low priority.
            sqlHandler.RegisterSource( "res-sql" );
            // Then registers "file-sql".
            sqlHandler.RegisterSource( SqlFileDiscoverer.DefaultSourceName );

            _engine.StartConfiguration.ScriptTypeManager.Register( sqlHandler );

            _engine.StartConfiguration.AddExplicitRegisteredClass( typeof( SqlDefaultDatabase ) );

            _engine.RegisterSetupEvent += OnRegisterSetup;
            
            return true;
        }

        /// <summary>
        /// Gets the engine to which this aspect is bound.
        /// </summary>
        public SetupEngine SetupEngine
        {
            get { return _engine; }
        }

        ISetupEngineAspectConfiguration ISetupEngineAspect.Configuration
        {
            get { return _config; }
        }

        /// <summary>
        /// Gets the configuration object.
        /// </summary>
        public SqlSetupAspectConfiguration Configuration
        {
            get { return _config; }
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
            return _sqlFileDiscoverer.DiscoverSqlFiles( String.Empty, SqlDefaultDatabase.DefaultDatabaseName, directoryPath, _sqlFiles, _engine.Scripts );
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
            var monitor = _engine.Monitor;

            bool hasError = false;
            using( monitor.OnError( () => hasError = true ) )
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
                        var items = new List<ISetupItem>();
                        foreach( var proto in _sqlFiles.OfType<SqlObjectProtoItem>() )
                        {
                            var item = proto.CreateItem( monitor, !_config.IgnoreMissingDependencyIsError, null );
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
            if( m == null ) _engine.Monitor.Warn().Send( "Database named '{0}' is not mapped.", dbName );
            return m;
        }

        ISqlManager ISqlManagerProvider.FindManagerByConnectionString( string conString )
        {
            if( conString == null ) throw new ArgumentNullException( "conString" );
            if( conString == _defaultDatabase.Connection.ConnectionString ) return _defaultDatabase;
            ISqlManager m = ObtainManagerByConnectionString( conString );
            if( m == null ) _engine.Monitor.Warn().Send( "Database connection to '{0}' is not mapped.", conString );
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
