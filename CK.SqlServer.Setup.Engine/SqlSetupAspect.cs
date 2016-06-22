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
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    public class SqlSetupAspect : ISetupEngineAspect, ISqlSetupAspect, IDisposable
    {
        readonly SqlSetupAspectConfiguration _config;
        readonly SetupEngine _engine;
        readonly SqlManagerProvider _databases;
        ISqlServerParser _sqlParser;
        ISetupItemParser _itemParser;
        ISqlManager _defaultDatabase;
        SqlFileDiscoverer _sqlFileDiscoverer;
        SetupItemCollector _sqlFiles;

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
        }

        bool ISetupEngineAspect.Configure()
        {
            _defaultDatabase = _databases.FindManagerByName( SqlDatabase.DefaultDatabaseName );
            _engine.StartConfiguration.VersionRepository = new SqlVersionedItemRepository( _defaultDatabase );
            _engine.StartConfiguration.SetupSessionMemoryProvider = new SqlSetupSessionMemoryProvider( _defaultDatabase );

            _engine.SetupableConfigurator = new ConfiguratorHook( this );
            var sqlHandler = new SqlScriptTypeHandler( _databases );
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
        public SetupEngine SetupEngine => _engine; 

        ISetupEngineAspectConfiguration ISetupEngineAspect.Configuration => _config; 

        /// <summary>
        /// Gets the configuration object.
        /// </summary>
        public SqlSetupAspectConfiguration Configuration => _config; 

        /// <summary>
        /// Gets the <see cref="ISqlServerParser"/> to use.
        /// </summary>
        public ISqlServerParser SqlParser
        {
            get
            {
                if( _sqlParser == null )
                {
                    Type t = SimpleTypeFinder.WeakResolver( "CK.SqlServer.Parser.SqlServerParser, CK.SqlServer.Parser", true );
                    _sqlParser = (ISqlServerParser)Activator.CreateInstance( t );
                }
                return _sqlParser;
            }
        }

        /// <summary>
        /// Gets the <see cref="ISetupItemParser"/>.
        /// </summary>
        public ISetupItemParser ItemParser => _itemParser ?? (_itemParser = new SqlItemParser( SqlParser ) );

        SqlFileDiscoverer EnsureFileDiscoverer()
        {
            if( _sqlFileDiscoverer == null )
            {
                _sqlFiles = new SetupItemCollector();
                _sqlFileDiscoverer = new SqlFileDiscoverer( ItemParser, _engine.Monitor );
            }
            return _sqlFileDiscoverer;
        }

        /// <summary>
        /// Discovers file packages (*.ck xml files) in given directory and sub directories.
        /// </summary>
        /// <param name="directoryPath">Directory from where *.ck files must be registered.</param>
        /// <returns>True if no error occurred. Errors are logged.</returns>
        public bool DiscoverFilePackages( string directoryPath )
        {
            return EnsureFileDiscoverer().DiscoverPackages( string.Empty, SqlDatabase.DefaultDatabaseName, directoryPath );
        }

        /// <summary>
        /// Discovers sql files in given directory and sub directories.
        /// </summary>
        /// <param name="directoryPath">Directory from where *.sql files must be registered.</param>
        /// <returns>True if no error occurred. Errors are logged.</returns>
        public bool DiscoverSqlFiles( string directoryPath )
        {
            return EnsureFileDiscoverer().DiscoverSqlFiles( string.Empty, SqlDatabase.DefaultDatabaseName, directoryPath, _sqlFiles, _engine.Scripts );
        }

        /// <summary>
        /// Gets the default database as a <see cref="SqlManager"/> object.
        /// </summary>
        public ISqlManager DefaultSqlDatabase => _defaultDatabase; 

        /// <summary>
        /// Gets the available databases (including the <see cref="DefaultSqlDatabase"/>).
        /// It is initialized with <see cref="SqlSetupAspectConfiguration.Databases"/> content but can be changed.
        /// </summary>
        public ISqlManagerProvider SqlDatabases => _databases; 

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
                if( !hasError && _sqlFiles != null )
                {
                    using( monitor.OpenInfo().Send( "Creating Sql Objects from {0} sql files.", _sqlFiles.Count ) )
                    {
                        e.Register( _sqlFiles );
                    }
                }
            }
            if( hasError )
            {
                e.CancelSetup( "Error while registering files." );
            }
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
